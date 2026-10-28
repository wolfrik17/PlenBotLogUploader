using Hardstuck.GuildWars2.MumbleLink;
using Hardstuck.Http;
using System;

namespace PlenBotLogUploader.Tools;

public interface ILogUploaderController
{
    void AddToText(string text);
    HttpClientController HttpClientController { get; }
    MumbleReader MumbleReader { get; }
    void ShowBalloon(string title, string description, int ms = 5000);
}
