# Serverside Polygons API for RAGE Multiplayer 1.1+

This resource provides the ability to create polygons on the server. These polygons tracking player and vehicle entities and can be used to create better shapes, which colshapes are not capable of. 

### Requirements
 - RAGE Multiplayer 1.1 and above.

### Installing [(Server API)](https://wiki.rage.mp/wiki/Development_with_Visual_Studio_Code)
- Add the [Polygons.cs](https://github.com/sckindvl/ragemp-polygons/blob/5e3836d7f0366df35bd8ed6fa5436c8a8169aac3/server/csharp/Polygons.cs) into your project
- In case you wanna use the 'visible' parameter, you have to setup the Client API aswell

### Installing [(Client API) ](https://wiki.rage.mp/wiki/Getting_Started_with_Client-side)
- Add your needed version into your client_packages whether you are using Java- or Typescript
### Notes
- ⚠️ It is not recommended to set a big amount of polygons visible in production environments as it could cause client performance issues.

### Example Usage
```cs
PolyAPI.Polygons.Create(new[]
{
    new Vector3(0, 0, 70),
    new Vector3(20, 0, 70),
    new Vector3(20, 20, 70),
    new Vector3(0, 20, 70),
}, 20, visible: true);
 
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

```

## Contributors
- Special thanks to [FatCatTuxedo](https://github.com/fatcattuxedo) for creating the Typescript file
