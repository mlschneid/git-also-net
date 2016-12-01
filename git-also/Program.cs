using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace git_also
{
    class Program
    {
        
        static void Main(string[] args)
        {
            string filename = args[0];

            if (String.IsNullOrWhiteSpace(filename))
            {
                Console.WriteLine("Please specify a file name");
                Environment.Exit(1);
            }

            if (!File.Exists(filename))
            {
                Console.WriteLine("That file does not exist");
                Environment.Exit(1);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                RedirectStandardOutput = true,
                FileName = "git",
                Arguments = $"log --pretty=format:\"%H\" {filename}",
                UseShellExecute = false
            };

            StringBuilder message = new StringBuilder();

            Process logPrettyPrint = Process.Start(startInfo);
            
            logPrettyPrint.OutputDataReceived += new DataReceivedEventHandler(
                delegate(object sender, DataReceivedEventArgs e)
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        message.AppendLine(e.Data);
                    }
                }
            );

            logPrettyPrint.BeginOutputReadLine();
            logPrettyPrint.WaitForExit();

            logPrettyPrint.Close();

            List<string> commitHashes = message.ToString().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

            // commit hash and related files.
            Dictionary<string, List <string>> hashToFileMapping = new Dictionary<string, List<string>>();

            foreach(string hash in commitHashes)
            {
                List<string> files = new List<string>();
                ProcessStartInfo processInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = "git",
                    Arguments = $"diff-tree --no-commit-id --name-only -r {hash}"
                };
                Process filesCommittedWithTarget = Process.Start(processInfo);

                filesCommittedWithTarget.OutputDataReceived += new DataReceivedEventHandler(
                    delegate (object sender, DataReceivedEventArgs e)
                    {
                        if (!String.IsNullOrEmpty(e.Data))
                        {
                           files.Add(e.Data);
                        }
                    }
                );

                filesCommittedWithTarget.BeginOutputReadLine();
                filesCommittedWithTarget.WaitForExit();

                hashToFileMapping.Add(hash, files);
            }

            // frequency
            Dictionary<string, int> frequency = new Dictionary<string, int>();
            foreach(string commit in hashToFileMapping.Keys)
            {
                foreach(string file in hashToFileMapping[commit])
                {
                    if (!frequency.ContainsKey(file))
                        frequency.Add(file, 1);
                    else
                        frequency[file] = frequency[file] + 1;   
                }
            }
            
            List<KeyValuePair<string, int>> sortedPairs = frequency.OrderBy(f => f.Value).ToList();
            sortedPairs.Reverse();

            int limit = System.Math.Min(frequency.Keys.Count, 10);

            Console.WriteLine($"{filename} was frequently checked in with:");

            foreach(var item in sortedPairs.Skip(1).Take(limit))
            {
                Console.WriteLine($"\t{item.Key} ({item.Value})");
            }
            
        }
    }
}
