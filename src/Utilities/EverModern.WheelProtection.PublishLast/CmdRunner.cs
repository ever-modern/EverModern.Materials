namespace EverModern.WheelProtection.PublishLast
{
    /// <summary>
    /// Runs shell commands using the Windows command processor.
    /// </summary>
    public static class CmdRunner
    {
        /// <summary>
        /// Executes a command and waits for completion.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        public static void Run(string command)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C {command}";
            process.StartInfo = startInfo;
            process.Start();

            process.OutputDataReceived += Process_OutputDataReceived;

            process.WaitForExit();
        }

        private static void Process_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}
