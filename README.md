# hybSClient: Multiplayer Client for Unity

The client is being build using [LiteNetLib](https://github.com/RevenantX/LiteNetLib). 
For WebGL, a modified version of [SimpleWebTransport](https://github.com/MirrorNetworking/SimpleWebTransport) is used.

Supports Windows, Mac, iOS, Android and WebGL.

Also, **hybSServer** repo can be found [here](https://github.com/rjproz/hybSServer)

## Features
1. Server key to avoid unauthorized clients
2. Game keys to avoid unauthorized clients
3. Theoretically, Unlimited Games and unlimited rooms in one server instance
4. Both Reliable and UnReliable Events
5. Room supports various parameters like game version, password, public/private mode and lock/unlock.
6. Reconnect and rejoin room feature.
7. Partially Authoritative MMO or Battle Royale logic can be implemented


The Game ["Hurl with Friends"](https://hybriona.itch.io/hurl-with-friends) was made using this library in 4 days for WeeklyGameJam 164
