using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

// Client app is the one sending messages to a Server/listener.
// Both listener and client can send messages back and forth once a
// communication is established.
public class SocketClient
{
    /// <summary>
    /// An IPEndPoint representing the destination target for the current message.
    /// </summary>
    private IPEndPoint destination;

    private double timeOut = 1;

    /// <summary>
    /// A message that is to be sent to the android tablet's server.
    /// </summary>
    private string message = "";

    public SocketClient(string message, IPEndPoint destination)
    {
        this.destination = destination;
        this.message = message;
    }

    /// <summary>
    /// Run two tasks in, one the supplied task the other a timeout delay. Returns a bool representing if
    /// the supplied task completed before the timeout task. 
    /// </summary>
    /// <param name="timeout">A double representing how long the timeout should be</param>
    private async Task<bool> TimeoutAfter(ValueTask task, double timeout)
    {

        using (var timeoutCancellationTokenSource = new CancellationTokenSource())
        {
            var completedTask = await Task.WhenAny(task.AsTask(), Task.Delay(TimeSpan.FromSeconds(timeout), timeoutCancellationTokenSource.Token));

            if (completedTask == task.AsTask())
            {
                timeoutCancellationTokenSource.Cancel();
                await task;  // Very important in order to propagate exceptions
                return true;
            }
            else
            {
                timeoutCancellationTokenSource.Cancel();
                return false;
            }
        }
    }

    /// <summary>
    /// Create a new TcpClient with the details collected from the initial server thread.
    /// Sends a message to the supplied EndPoint with details about certain actions to be taken,
    /// outputs or machine states.
    /// </summary>
    public bool send()
    {
        TcpClient? client = null;

        try
        {
            // Create a TCP client and connect via the supplied endpoint.
            client = new TcpClient();

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            ValueTask connect = client.ConnectAsync(destination.Address, destination.Port, token);
            Task<bool> task = TimeoutAfter(connect, timeOut);

            if (!task.Result)
            {
                tokenSource.Cancel();
                Console.WriteLine("Socket timeout: " + destination.Address);
                throw new SocketException();
            };

            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                if (client.Client.RemoteEndPoint == null)
                {
                    return false;
                }

                // Get a client stream for reading and writing.
                NetworkStream stream = client.GetStream();

                //Logger.WriteLog($"Socket connected to {client.Client.RemoteEndPoint.ToString()}", writeToLog);

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(this.message);

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                //Logger.WriteLog($"Sent: {message}", writeToLog);

                // Close everything.
                stream.Close();
                client.Close();
                return true;
            }
            catch (ArgumentNullException ane)
            {
                //Logger.WriteLog($"ArgumentNullException : {ane.ToString()}");
                return false;
            }
            catch (SocketException se)
            {
                //Logger.WriteLog($"SocketException : {se.ToString()}");
                return false;
            }
            catch (Exception e)
            {
                //Logger.WriteLog($"Unexpected exception : {e.ToString()}");
                return false;
            }
        }
        catch (Exception e)
        {
            if (client != null) {
                client.Dispose();
                client.Close();
            }

            //Logger.WriteLog($"Unexpected exception : {e.ToString()}");
        }
        finally
        {
            //Write the logs to text
            //Logger.WorkQueue();
        }

        return false;
    }
}
