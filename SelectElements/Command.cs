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
            Document doc = uidoc.Document;

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


                        sb.AppendLine(e.Name);
                    }
                    TaskDialog.Show("title: ", sb.ToString());
                }
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
}
