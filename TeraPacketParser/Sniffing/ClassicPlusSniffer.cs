// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// TERA Europe Classic+ mirror-socket sniffer.
//
// Connects to 127.0.0.1:7803 over TCP and reads framed, pre-encrypted packets
// from a local mirror exposed by the Noctenium proxy / DLL. Every frame is
// [u16 totalLen][u8 direction][payload], little-endian, totalLen inclusive of
// the direction byte. direction=1 → client-to-server, direction=2 →
// server-to-client. Payloads are still TERA-session-encrypted; the proxy is
// responsible for replaying the session-key exchange as the first two frames
// after each connect. This class decrypts every frame locally via
// ConnectionDecrypter keyed to the "EUC" region.
//
// Ported from LukasTD/ShinraMeter@EU-Classic by the Classic+ fork; see
// ../../DESIGN.md "Shinra / TCC connection mechanism (locked)" for the full
// protocol.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TeraPacketParser.TeraCommon.Game;
using TeraPacketParser.TeraCommon.Sniffing;

namespace TeraPacketParser.Sniffing;

public class ClassicPlusSniffer : ITeraSniffer
{
    private readonly string _socketHost;
    private readonly int _socketPort;

    private ConnectionDecrypter? _decrypter;
    private MessageSplitter? _messageSplitter;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private bool _enabled;
    private bool _connected;

    public event Action<Message>? MessageReceived;
    public event Action<Server>? NewConnection;
    public event Action? EndConnection;

    public ConcurrentQueue<Message> Packets { get; } = new();

    public ClassicPlusSniffer() : this("127.0.0.1", 7803) { }

    public ClassicPlusSniffer(string host, int port)
    {
        _socketHost = host;
        _socketPort = port;
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;

            if (_enabled)
            {
                if (_loopTask is null || _loopTask.IsCompleted)
                {
                    _cts = new CancellationTokenSource();
                    _loopTask = Task.Run(() => LoopAsync(_cts.Token));
                }
            }
            else
            {
                _cts?.Cancel();
            }
        }
    }

    public bool Connected
    {
        get => _connected;
        set
        {
            if (_connected == value) return;
            _connected = value;
            if (!_connected) EndConnection?.Invoke();
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(_socketHost, _socketPort);
                Connected = true;
                var stream = client.GetStream();

                // Region "EUC" selects the Classic+ decryption key schedule and
                // opcode map family. Server display name "Classic+" identifies
                // the private-server variant in logs.
                var server = new Server("Classic+", "EUC", _socketHost);
                _decrypter = new ConnectionDecrypter(server.Region);
                _decrypter.ClientToServerDecrypted += HandleClientToServerDecrypted;
                _decrypter.ServerToClientDecrypted += HandleServerToClientDecrypted;

                _messageSplitter = new MessageSplitter();
                _messageSplitter.MessageReceived += HandleMessageReceived;

                NewConnection?.Invoke(server);

                var lenBuf = new byte[2];
                while (!token.IsCancellationRequested)
                {
                    if (!await ReadExactAsync(stream, lenBuf, 2, token)) break;
                    var totalLen = BitConverter.ToUInt16(lenBuf, 0);
                    if (totalLen < 1) continue;

                    var dirBuf = new byte[1];
                    if (!await ReadExactAsync(stream, dirBuf, 1, token)) break;
                    byte direction = dirBuf[0];

                    var payloadLen = totalLen - 1;
                    var payload = new byte[payloadLen];
                    if (payloadLen > 0 && !await ReadExactAsync(stream, payload, payloadLen, token)) break;

                    switch (direction)
                    {
                        case 1: _decrypter.ClientToServer(payload, 0); break;
                        case 2: _decrypter.ServerToClient(payload, 0); break;
                        default:
                            // Unknown direction byte. Skip silently — a proxy
                            // upgrade may add new sub-streams later.
                            break;
                    }
                }
            }
            catch
            {
                // Reset on any socket/stream exception; reconnect loop handles retry.
            }
            finally
            {
                try { client?.Close(); } catch { }
                if (Connected) Connected = false;
            }

            if (token.IsCancellationRequested) break;
            try { await Task.Delay(2000, token); } catch (TaskCanceledException) { break; }
        }
    }

    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
    {
        int read = 0;
        while (read < count)
        {
            var got = await stream.ReadAsync(buffer.AsMemory(read, count - read), token);
            if (got == 0) return false;
            read += got;
        }
        return true;
    }

    private void HandleMessageReceived(Message message)
    {
        Packets.Enqueue(message);
        MessageReceived?.Invoke(message);
    }

    private void HandleClientToServerDecrypted(byte[] data)
    {
        _messageSplitter?.ClientToServer(DateTime.UtcNow, data);
    }

    private void HandleServerToClientDecrypted(byte[] data)
    {
        _messageSplitter?.ServerToClient(DateTime.UtcNow, data);
    }
}
