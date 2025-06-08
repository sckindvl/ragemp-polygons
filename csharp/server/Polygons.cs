using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using Vector2 = System.Numerics.Vector2;

namespace PolyAPI
{
    public enum PolygonEvent
    {
        OnPlayerEnter,
        OnPlayerLeave,
        OnVehicleEnter,
        OnVehicleLeave
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PolygonAttribute : Attribute
    {
        public PolygonEvent EventType { get; }
        public PolygonAttribute(PolygonEvent eventType) => EventType = eventType;
    }

    public class Polygon
    {
        public int Id { get; set; }
        public Vector3[] Vertices { get; set; }
        public float Height { get; set; }
        public uint Dimension { get; set; }
        public bool Visible { get; set; }
        public bool DrawBox { get; set; }
        public int LineAmount { get; set; }
        public uint[] LineColorRGBA { get; set; }
        internal DummyEntity Dummy { get; set; }
        public Vector3 Center { get; private set; }
        public float BoundingRadius { get; private set; }

        public void SetData(string key, object value) => Dummy?.SetData(key, value);
        public T GetData<T>(string key) => Dummy != null && Dummy.HasData(key) ? Dummy.GetData<T>(key) : default;
        public bool HasData(string key) => Dummy != null && Dummy.HasData(key);

        public void SetSharedData(string key, object value) => Dummy?.SetSharedData(key, value);
        public T GetSharedData<T>(string key) => Dummy != null && Dummy.HasSharedData(key) ? Dummy.GetSharedData<T>(key) : default;
        public bool HasSharedData(string key) => Dummy != null && Dummy.HasSharedData(key);

        public void RecalculateBounds(float extraRadius = 20f)
        {
            if (Vertices == null || Vertices.Length == 0) return;
            Center = new Vector3(
                Vertices.Average(v => v.X),
                Vertices.Average(v => v.Y),
                Vertices.Average(v => v.Z)
            );
            BoundingRadius = Vertices.Max(v =>
                (float)Math.Sqrt(
                    Math.Pow(v.X - Center.X, 2) +
                    Math.Pow(v.Y - Center.Y, 2) +
                    Math.Pow(v.Z - Center.Z, 2)
                )
            ) + extraRadius;
        }
    }

    public static class Polygons
    {
        static Polygons() => RegisterRageEventHandlers();

        public static List<Polygon> Pool { get; } = new List<Polygon>();
        private static int _lastId;

        public static event Action<Player, Polygon> PlayerEnter;
        public static event Action<Player, Polygon> PlayerLeave;
        public static event Action<Vehicle, Polygon> VehicleEnter;
        public static event Action<Vehicle, Polygon> VehicleLeave;

        private static readonly Dictionary<Player, HashSet<int>> PlayerState = new Dictionary<Player, HashSet<int>>();
        private static readonly Dictionary<Vehicle, HashSet<int>> VehicleState = new Dictionary<Vehicle, HashSet<int>>();

        private const double TWOPI = Math.PI * 2;
        private const double EPSILON = 1e-7;

        public static Polygon Create(Vector3[] vertices, float height, uint dimension = 0, bool visible = false, bool drawBox = false, int lineAmount = 10, uint[] lineColorRGBA = null)
        {
            var dummy = NAPI.DummyEntity.CreateDummyEntity(0, new Dictionary<string, object>(), dimension);
            var poly = new Polygon
            {
                Id = Interlocked.Increment(ref _lastId),
                Vertices = vertices,
                Height = height,
                Dimension = dimension,
                Visible = visible,
                DrawBox = drawBox,
                LineAmount = lineAmount,
                LineColorRGBA = lineColorRGBA ?? new uint[] { 255, 255, 255, 255 },
                Dummy = dummy
            };
            poly.RecalculateBounds(20f);
            Pool.Add(poly);
            return poly;
        }

        public static void Destroy(Polygon poly)
        {
            if (poly.Dummy != null)
            {
                poly.Dummy.Delete();
                poly.Dummy = null;
            }
            Pool.Remove(poly);
        }

        public static void Tick()
        {
            UpdateEntities(NAPI.Pools.GetAllPlayers(), PlayerState, PlayerEnter, PlayerLeave, p => p.Position, p => p.Dimension);
            UpdateEntities(NAPI.Pools.GetAllVehicles(), VehicleState, VehicleEnter, VehicleLeave, v => v.Position, v => v.Dimension);
        }

        private static void UpdateEntities<T>(
            IEnumerable<T> entities,
            Dictionary<T, HashSet<int>> state,
            Action<T, Polygon> onEnter,
            Action<T, Polygon> onLeave,
            Func<T, Vector3> getPos,
            Func<T, uint> getDim)
        {
            foreach (var ent in entities)
            {
                if (!state.TryGetValue(ent, out var inside))
                {
                    inside = new HashSet<int>();
                    state[ent] = inside;
                }
                var pos = getPos(ent);
                var dim = getDim(ent);

                foreach (var poly in Pool)
                {
                    var isInside = IsInside(pos, poly, dim);
                    if (isInside)
                    {
                        if (inside.Add(poly.Id))
                            onEnter?.Invoke(ent, poly);
                    }
                    else
                    {
                        if (inside.Remove(poly.Id))
                            onLeave?.Invoke(ent, poly);
                    }
                }
            }
        }

        private static bool IsInside(Vector3 pos, Polygon poly, uint dimension)
        {
            if (dimension != poly.Dimension)
                return false;

            var dx = pos.X - poly.Center.X;
            var dy = pos.Y - poly.Center.Y;
            var dz = pos.Z - poly.Center.Z;
            var dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (dist > poly.BoundingRadius)
                return false;

            var vertices = poly.Vertices;
            var zValid = vertices.Any(v => pos.Z >= v.Z && pos.Z <= v.Z + poly.Height);
            var angleSum = GetAngleSum(pos, vertices) >= 5.8;

            if (!(zValid || angleSum))
                return false;

            var poly2D = vertices.Select(v => new Vector2(v.X, v.Y)).ToArray();
            return PointInPoly(new Vector2(pos.X, pos.Y), poly2D);
        }

        private static bool PointInPoly(Vector2 point, Vector2[] poly)
        {
            bool inside = false;
            int j = poly.Length - 1;
            for (int i = 0; i < poly.Length; j = i++)
            {
                if (((poly[i].Y > point.Y) != (poly[j].Y > point.Y)) &&
                    (point.X < (poly[j].X - poly[i].X) * (point.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X))
                    inside = !inside;
            }
            return inside;
        }

        private static double Modulus(Vector3 p) => Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);

        private static double GetAngleSum(Vector3 pos, Vector3[] vertices)
        {
            double sum = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                var p1 = new Vector3(vertices[i].X - pos.X, vertices[i].Y - pos.Y, vertices[i].Z - pos.Z);
                var next = vertices[(i + 1) % vertices.Length];
                var p2 = new Vector3(next.X - pos.X, next.Y - pos.Y, next.Z - pos.Z);

                var m1 = Modulus(p1);
                var m2 = Modulus(p2);
                if (m1 <= EPSILON || m2 <= EPSILON) return TWOPI;
                var costheta = (p1.X * p2.X + p1.Y * p2.Y + p1.Z * p2.Z) / (m1 * m2);
                sum += Math.Acos(costheta);
            }
            return sum;
        }

        private static void RegisterRageEventHandlers()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
            foreach (var type in types)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var attr = method.GetCustomAttribute<PolygonAttribute>();
                    if (attr == null) continue;
                    switch (attr.EventType)
                    {
                        case PolygonEvent.OnPlayerEnter:
                            PlayerEnter += (Action<Player, Polygon>)Delegate.CreateDelegate(typeof(Action<Player, Polygon>), method);
                            break;
                        case PolygonEvent.OnPlayerLeave:
                            PlayerLeave += (Action<Player, Polygon>)Delegate.CreateDelegate(typeof(Action<Player, Polygon>), method);
                            break;
                        case PolygonEvent.OnVehicleEnter:
                            VehicleEnter += (Action<Vehicle, Polygon>)Delegate.CreateDelegate(typeof(Action<Vehicle, Polygon>), method);
                            break;
                        case PolygonEvent.OnVehicleLeave:
                            VehicleLeave += (Action<Vehicle, Polygon>)Delegate.CreateDelegate(typeof(Action<Vehicle, Polygon>), method);
                            break;
                    }
                }
            }
        }
    }

    public class PolygonScript : Script
    {
        private const int CheckInterval = 100;

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart()
        {
            NAPI.Task.Run(() => Polygons.Tick(), CheckInterval);
        }

        [ServerEvent(Event.PlayerConnected)]
        public void OnPlayerConnected(Player player)
        {
            var list = Polygons.Pool
                .Where(p => p.Visible)
                .Select(p => new
                {
                    id = p.Id,
                    vertices = p.Vertices.Select(v => new { x = v.X, y = v.Y, z = v.Z }).ToArray(),
                    height = p.Height,
                    dimension = p.Dimension,
                    visible = p.Visible,
                    drawBox = p.DrawBox,
                    lineAmount = p.LineAmount,
                    lineColorRGBA = p.LineColorRGBA
                })
                .ToList();
            var json = NAPI.Util.ToJson(list);
            NAPI.ClientEvent.TriggerClientEvent(player, "Polygons:API:init", json);
        }
    }
}
