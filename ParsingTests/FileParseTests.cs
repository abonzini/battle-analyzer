using BattleAnalyzer;

namespace ParsingTests
{
    [TestClass]
    public class FileParseTests
    {
        [TestMethod]
        public void ParseAllFiles()
        {
            string directory = ".\\..\\..\\..\\..\\replays";

            foreach (string filePath in Directory.GetFiles(directory))
            {
                if(Path.GetExtension(filePath) == ".html")
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                    string correspondingFile = Path.Combine(directory, fileNameWithoutExtension + ".txt");

                    if (File.Exists(correspondingFile))
                    {
                        string replayInfo = File.ReadAllText(correspondingFile);
                        string newReplayInfo = BattleAnalyzer.Program.AnalyzeBattle(filePath);
                        Assert.AreEqual(newReplayInfo, replayInfo);
                    }
                }
            }

        }
    }
}