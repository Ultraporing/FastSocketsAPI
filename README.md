It's an multi-threaded TCP Networking API written in C# NET 3.5 which makes it Unity3D compatible. 

It provides events for the builtin Packets and API Events.

A Custom Logger can create Logfiles optionally, it displays API messages and errors with Stacktrace. 

This API allows you to create your packets your self, and it will call the corresponding Received methods via Reflection.

The Reflection calls are really fast since it uses caching on creation.

Documentation is in this Folder: FastSockets/Help