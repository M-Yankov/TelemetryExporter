using Dynastream.Fit;

namespace TelemetryExporter.Core.Utilities
{
    public class FitDecoder
    {
        public FitDecoder(string path)
        {
            FileInfo fitFile = new(path);
            using FileStream fitStream = fitFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            FitMessages = DecodeMessages(fitStream);
        }

        public FitMessages FitMessages { get; private set; }

        public FitDecoder(Stream fitStream)
        {
            FitMessages = DecodeMessages(fitStream);
        }

        private static FitMessages DecodeMessages(Stream fitStream)
        {
            Decode decode = new();
            FitListener fitListener = new();
            decode.MesgEvent += fitListener.OnMesg;
            decode.Read(fitStream);

            return fitListener.FitMessages;
        }
    }
}
