using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.Creation;
using Application = Autodesk.Revit.ApplicationServices.Application;
using System.Diagnostics;

namespace SelectElements
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Autodesk.Revit.DB.Document doc = uidoc.Document;

            IList<Reference> pickedObjs = null;
            // pickedObjs = uidoc.Selection.PickObjects(ObjectType.Element, "Select Elements");
            Reference pickedref = uidoc.Selection.PickObject(ObjectType.Element, "Please select a element");

            List<Reference> customPick = new List<Reference>
            {
                pickedref,
            };
            pickedObjs = customPick;

            List<ElementId> ids = (from Reference r in pickedObjs select r.ElementId).ToList();

            using (Transaction tx = new Transaction(doc))
            {
                StringBuilder sb = new StringBuilder();
                tx.Start("transaction");
                if (pickedObjs != null && pickedObjs.Count > 0)
                {
                    foreach (ElementId eid in ids)
                    {
                        Element e = doc.GetElement(eid);
                        // Wall wall = e as Wall;

                        List<Material> mats = e.GetMaterialIds(false).ToList().Select(x => doc.GetElement(x) as Material).ToList();
                        // ref: https://forums.autodesk.com/t5/revit-api-forum/getting-compoundstructure-of-wall-in-rvtlink/m-p/9615171/highlight/true#M48205

                        if (e is Wall)
                        {
                            CompoundStructure compoundStructure = (e as Wall).WallType.GetCompoundStructure();

                            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();

                            foreach (CompoundStructureLayer layer in layers)
                            {
                                // Retrieve Faces
                            }
                        }
                        else if(e is FamilyInstance)
                        {

                            FamilyInstance famInst = doc.GetElement(new ElementId(335742)) as FamilyInstance;
                            Family family = famInst.Symbol.Family;
                            Autodesk.Revit.DB.Document famDoc = doc.EditFamily(family);
                            //
                            // example: find all generic form elements within the family document
                            FilteredElementCollector coll = new FilteredElementCollector(famDoc);
                            int cout = coll.OfClass(typeof(GenericForm)).ToElementIds().Count;
                            TaskDialog.Show("Element Info", string.Format("{0} elements within family", cout));
                            //
                            // close family document after iteration
                            famDoc.Close(false);



                            // reference: https://stackoverflow.com/a/30338440/6908282
                            List<Element> listFamilyInstances = new FilteredElementCollector(doc, doc.ActiveView.Id)
                                                                .OfClass(typeof(FamilyInstance))
                                                                .Cast<FamilyInstance>()
                                                                .Where(a => a.SuperComponent == null)
                                                                .SelectMany(a => a.GetSubComponentIds())
                                                                .Select(a => doc.GetElement(a))
                                                                .ToList();

                            FamilyInstance aFamilyInst = e as FamilyInstance;
                            // we need to skip nested family instances 
                            // since we already get them as per below
                            if (aFamilyInst.SuperComponent == null)
                            {
                                // reference: https://stackoverflow.com/a/29339317/6908282
                                // this is a family that is a root family
                                // ie might have nested families 
                                // but is not a nested one
                                var subElements = aFamilyInst.GetSubComponentIds();
                                if (subElements.Count == 0)
                                {
                                    // no nested families
                                    System.Diagnostics.Debug.WriteLine(aFamilyInst.Name + " has no nested families");
                                }
                                else
                                {
                                    // has nested families
                                    foreach (var aSubElemId in subElements)
                                    {
                                        var aSubElem = doc.GetElement(aSubElemId);
                                        if (aSubElem is FamilyInstance)
                                        {
                                            sb.AppendLine(aSubElem.Name + " is a nested family of " + aFamilyInst.Name);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var opt = doc.Application.Create.NewGeometryOptions();
                            GeometryElement geo = e.get_Geometry(opt);

                            int test = ExportSolids(e, opt);


                            if (geo != null)
                            {
                                if (geo.MaterialElement == null)
                                {
                                    //return null;
                                }
                                else
                                {
                                    var matElem = geo.MaterialElement;
                                    // return matElem;
                                }
                            }
                        }

                        sb.AppendLine(e.Name);
                    }
                    TaskDialog.Show("title: ", sb.ToString());
                }
                tx.Commit();
            }
            return Result.Succeeded;
        }

        int ExportSolids(Element e,Options opt)
        {
            int nSolids = 0;
            Transform currentDocTransform = Transform.Identity;

            GeometryElement geometryElement = e.get_Geometry(opt);

            if (geometryElement != null)
            {
                GeometryElement transformedGeometryElement = geometryElement.GetTransformed(Transform.Identity);
                Solid solid;

                if (null != transformedGeometryElement)
                {
                    Autodesk.Revit.DB.Document doc = e.Document;

                    if (e is FamilyInstance)
                    {
                        transformedGeometryElement = transformedGeometryElement.GetTransformed(Transform.Identity);
                    }

                    GeometryInstance inst = null;

                    foreach (GeometryObject obj in transformedGeometryElement)
                    {
                        if (e is TopographySurface)
                        {
                            Mesh mesh = obj as Mesh;
                            if (mesh != null)
                            {
                                if (null != mesh
                                    && 0 < mesh.Vertices.Count)
                                {
                                    ++nSolids;
                                }
                            }
                        }
                        else
                        {
                            solid = obj as Solid;
                            if (null != solid
                                && 0 < solid.Faces.Size)
                            {
                                ++nSolids;
                            }
                            else
                            {
                                inst = obj as GeometryInstance;
                                if (0 == nSolids && null != inst)
                                {
                                    transformedGeometryElement = inst.GetSymbolGeometry().GetTransformed(Transform.Identity);
                                    foreach (GeometryObject oo in transformedGeometryElement)
                                    {
                                        solid = oo as Solid;

                                        if (null != solid
                                          && 0 < solid.Faces.Size)
                                        {
                                            ++nSolids;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return nSolids;
        }

        List<string> ListElementsInAssembly(Autodesk.Revit.DB.Document doc)
        {
            // 'FP Description' shared parameter GUID

            Guid guid = new Guid(
              "ac6ed937-ffb7-4b18-9c69-7541f5c0319d");

            FilteredElementCollector assemblies
              = new FilteredElementCollector(doc)
                .OfClass(typeof(AssemblyInstance));

            List<string> descriptions = new List<string>();

            int n;
            string s;

            foreach (AssemblyInstance a in assemblies)
            {
                ICollection<ElementId> ids = a.GetMemberIds();

                n = ids.Count;

                //s = string.Format(
                //  "\r\nAssembly {0} has {1} member{2}{3}",
                //  a.get_Parameter(guid).AsString(),
                //  n, Util.PluralSuffix(n), Util.DotOrColon(n));

                //descriptions.Add(s);

                n = 0;

                foreach (ElementId id in ids)
                {
                    Element e = doc.GetElement(id);

                    descriptions.Add(string.Format("{0}: {1}",
                      n++, e.get_Parameter(guid).AsString()));
                }
            }

            Debug.Print(string.Join("\r\n", descriptions));

            return descriptions;
        }
    }
}
