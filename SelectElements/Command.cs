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

            pickedObjs.Add(pickedref);

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
