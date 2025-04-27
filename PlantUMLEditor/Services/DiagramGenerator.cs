using System;
using System.Diagnostics;
using System.IO;

namespace PlantUMLEditor.Services
{
    internal class DiagramGenerator
    {
        public static string GeneratePlantUmlDiagram(string umlCode, string _outputDirectory, string _plantUmlJarPath)
        {
            Directory.CreateDirectory(_outputDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string umlFilePath = Path.Combine(_outputDirectory, $"diagram_{timestamp}.puml");
            string outputFilePath = Path.Combine(_outputDirectory, $"diagram_{timestamp}.png");

            File.WriteAllText(umlFilePath, umlCode);

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-DPLANTUML_LIMIT_SIZE=8192 -jar \"{_plantUmlJarPath}\" \"{umlFilePath}\" -o \"{_outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();


            if (!File.Exists(outputFilePath))
            {
                throw new Exception("Dijagram nije generiran. Detalji: " + error);
            }

            return outputFilePath;
        }

        public static string ExportPlantUmlDiagram(string umlCode, string format, string _outputDirectory, string _plantUmlJarPath)
        {
            Directory.CreateDirectory(_outputDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string umlFilePath = Path.Combine(_outputDirectory, $"diagram_{timestamp}.puml");

            string fileExtension = format;

            string outputFilePath = Path.Combine(_outputDirectory, $"diagram_{timestamp}.{fileExtension}");

            File.WriteAllText(umlFilePath, umlCode);

            string formatArg = format == "png" ? "" : $"-t{format}";

            string plantUmlDir = Path.GetDirectoryName(_plantUmlJarPath);

            ProcessStartInfo startInfo;

            if (format == "pdf")
            {
                string classpath = $"\"{_plantUmlJarPath}\"";

                foreach (string jarFile in Directory.GetFiles(plantUmlDir, "*.jar"))
                {
                    if (Path.GetFileName(jarFile) != Path.GetFileName(_plantUmlJarPath))
                    {
                        classpath += $";\"{jarFile}\"";
                    }
                }

                startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-Djava.awt.headless=true -cp {classpath} net.sourceforge.plantuml.Run {formatArg} \"{umlFilePath}\" -o \"{_outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{_plantUmlJarPath}\" {formatArg} \"{umlFilePath}\" -o \"{_outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            Process process = new Process { StartInfo = startInfo };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!File.Exists(outputFilePath))
            {
                throw new Exception($"Dijagram nije generiran. Detalji: {error}");
            }

            return outputFilePath;
        }
    }
}
