# Serverside Polygons API for RAGE Multiplayer DP-1.2
### This resource adds the ability to create Polygons which are working the same way as Colshapes, but are faster and have more events.

### Example usage:
```cs
# Events
[Polygon(PolygonEvent.OnPlayerEnter)]
public static void PlayerEnter(Player player, Polygon poly)
{
    Console.WriteLine("Player (ID: " + player.Id + ") entered a polygon.");
}

[Polygon(PolygonEvent.OnPlayerLeave)]
public static void PlayerLeave(Player player, Polygon poly)
{
    Console.WriteLine("Player (ID: " + player.Id + ") left a polygon.");
}

[Polygon(PolygonEvent.OnVehicleEnter)]
public static void VehicleEnter(Vehicle vehicle, Polygon poly)
{
    Console.WriteLine("Vehicle " + vehicle.DisplayName + " entered a polygon.");
}
        
[Polygon(PolygonEvent.OnVehicleLeave)]
public static void VehicleLeave(Vehicle vehicle, Polygon poly)
{
    Console.WriteLine("Vehicle " + vehicle.DisplayName + " left a polygon.");
}

# Create a polygon
PolyAPI.Polygons.Create(new[]
{
    new Vector3(0, 0, 70),
    new Vector3(20, 0, 70),
    new Vector3(20, 20, 70),
    new Vector3(0, 20, 70),
}, 20, visible: true);

// Note: In case you wanna use visible: true, you have to implement the Client JS API from
// https://github.com/sckindvl/polygons
```
