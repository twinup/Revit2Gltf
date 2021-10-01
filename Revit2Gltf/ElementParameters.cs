using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    public class ElementParameters
    {

        public static HashSet<string> Session_Params = new HashSet<string>();

        public static HashSet<string> Rooms = new HashSet<string>();

        readonly string Id;

        public readonly string Category;

        public Dictionary<string, string> Parameters => _parameters.ToDictionary(k => k.Key, k => k.Value);
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();

        public ElementParameters(Element e, bool calculateCentroid = true)
        {
            Id = e.UniqueId;
            Category = e.Category.Name;

            _parameters.Add("Name", e.Name);

            Siphon(e,calculateCentroid);
        }

        private void Siphon(Element e, bool calculateCentroid)
        {
            // Base params
            IEnumerable<Parameter> ps =
                e.GetOrderedParameters()
                .Where(p => p.HasValue && !String.IsNullOrEmpty(p.AsValueString()));

            foreach(var p in ps)
            {
                string name = p.Definition.Name;
                Session_Params.Add(name);

                if (!_parameters.ContainsKey(name))
                {
                    _parameters.Add(name, p.AsValueString());
                }
                else
                {
                    _parameters[name] = p.AsValueString();
                }
            }

            // Centroid
            CentroidVolume c = null;
            if (calculateCentroid && e.TryGetCentroid(Extensions.DefaultExportGeometryOptions, out c))
            {
                _parameters.Add("Centroid", c.Centroid_Str);
            }

            // Room
            if(TryGetRoom(e, c, out var room, out var toroom, out var fromroom))
            {
                if (room != null) { _parameters.Add("Room", room.UniqueId);             Rooms.Add(room.UniqueId); }
                if (toroom != null) { _parameters.Add("ToRoom", toroom.UniqueId);       Rooms.Add(toroom.UniqueId); }
                if (fromroom != null) { _parameters.Add("FromRoom", fromroom.UniqueId); Rooms.Add(fromroom.UniqueId); }
            }
        }

        public bool TryGetRoom(Element e, CentroidVolume centroid, out Room room, out Room toRoom, out Room fromRoom)
        {
            room = null;
            toRoom = null;
            fromRoom = null;
            var fi = e as FamilyInstance;
            if(fi == null) { goto tryLocationBased; }
            room = fi.Room;
            toRoom = fi.FromRoom;
            fromRoom = fi.ToRoom;
            return !(room == null && toRoom == null && fromRoom == null);

        tryLocationBased:
            if(centroid == null) { return false; }
            room = e.Document.GetRoomAtPoint(centroid.Centroid);
            return room != null;
        }

        public void Add(string paramName, object value)
        {
            if (!_parameters.ContainsKey(paramName))
            {
                _parameters.Add(paramName, value.ToString());
            }
            else
            {
                // Overrite
                _parameters[paramName] = value.ToString();
            }
        }
    }
}
