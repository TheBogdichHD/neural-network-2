using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Accord.WindowsForms;
using System.IO;

namespace NeuralNetwork1
{
    class TLGBotik
    {
        public Telegram.Bot.TelegramBotClient botik = null;

        private BaseNetwork perseptron = null;
        private MagicEye magicEye = null;

        private string[] classLabels;

        public TLGBotik(BaseNetwork net, Settings settings, string[] labels)
        {
            magicEye = new MagicEye();
            magicEye.settings = settings;
            magicEye.settings.processImg = true;
            magicEye.settings.border = 160;
            classLabels = labels;
            perseptron = net;
            var key = System.IO.File.ReadAllText("..\\..\\botKey.txt");
            botik = new TelegramBotClient(key);
            botik.OnMessage += OnMessage;
        }
        async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Type == MessageType.Photo)
            {
                var fileId = msg.Photo.Last().FileId;
                var fileInfo = await botik.GetFile(fileId);
                var filePath = fileInfo.FilePath;
                
                var stream = new MemoryStream();
                await botik.DownloadFile(filePath, stream);
                var img = System.Drawing.Image.FromStream(stream);
                magicEye.ProcessImage(new System.Drawing.Bitmap(img));
                var sample = new Sample(magicEye.sensors, 7);
                perseptron.Predict(sample);

                await botik.SendMessage(msg.Chat, $"{msg.Chat.Username}: {classLabels[sample.recognizedClass]}");
            }
        }

    }
}
