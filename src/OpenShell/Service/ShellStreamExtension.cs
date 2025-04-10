using Renci.SshNet;
using System.Reflection;

namespace OpenShell.Service;

public static class ShellStreamExtension
{
    public static void SendWindowChangeRequest(this ShellStream stream, uint cols, uint rows, uint width, uint height)
    {
        // The shell stream is a private class, so we need
        // to use reflection to access its private fields and methods
        // The strategy is to get the _channel field from the ShellStream, and then
        // get the SendWindowChangeRequest method from the _channel field
        // for submitting low level requests to the shell stream to change its window size at runtime

        // get _channel from ShellStream by reflection
        var _channel =
            stream.GetType().GetField("_channel",
                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(stream);

        // get SendWindowChangeRequest method from _channel by reflection
        var method =
            _channel?.GetType().GetMethod("SendWindowChangeRequest",
                BindingFlags.Public | BindingFlags.Instance);

        // invoke SendWindowChangeRequest method for change cols, rows, width and height of the shell stream
        method?.Invoke(_channel, new object[] { cols, rows, width, height });
    }
}