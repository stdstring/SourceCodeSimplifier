using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;

namespace SourceCodeSimplifierConceptTests
{
    internal static class DirectoryUtils
    {
        public static void CopyDirectory(String source, String dest, Boolean overwriteContent)
        {
            DirectoryInfo destDirectory = new DirectoryInfo(dest);
            Boolean isDestEmpty = destDirectory.Exists &&
                                  (destDirectory.GetFiles().Length == 0) &&
                                  (destDirectory.GetDirectories().Length == 0);
            if (!isDestEmpty && !overwriteContent)
                throw new InvalidOperationException("Nonempty dest directory");
            if (destDirectory.Exists)
                destDirectory.Delete(true);
            destDirectory.Create();
            Copy(source, destDirectory.FullName);
        }

        private static void Copy(String sourceRoot, String destRoot)
        {
            foreach (String sourceFile in Directory.GetFiles(sourceRoot))
            {
                String fileName = Path.GetFileName(sourceFile);
                String destFile = Path.Combine(destRoot, fileName);
                File.Copy(sourceFile, destFile);
            }
            foreach (String sourceDirectory in Directory.GetDirectories(sourceRoot))
            {
                String directoryName = Path.GetFileName(sourceDirectory);
                String destDirectory = Path.Combine(destRoot, directoryName);
                Directory.CreateDirectory(destDirectory);
                Copy(sourceDirectory, destDirectory);
            }
        }
    }

    internal static class PrerequisitesManager
    {
        public static void Run()
        {
            // usage of MSBuild
            MSBuildLocator.RegisterDefaults();
        }
    }

    internal static class CompilationChecker
    {
        public static Boolean CheckCompilationErrors(String filename, Compilation compilation)
        {
            Console.WriteLine("Checking compilation for errors and warnings:");
            IList<Diagnostic> diagnostics = compilation.GetDiagnostics();
            Diagnostic[] diagnosticErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            Diagnostic[] diagnosticWarnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToArray();
            Boolean hasErrors = false;
            Console.WriteLine($"Found {diagnosticErrors.Length} errors in the compilation");
            foreach (Diagnostic diagnostic in diagnosticErrors)
            {
                Console.WriteLine($"Found following error in the compilation of the {filename} entity: {diagnostic.GetMessage()}");
                hasErrors = true;
            }
            Console.WriteLine($"Found {diagnosticWarnings.Length} warnings in the compilation");
            foreach (Diagnostic diagnostic in diagnosticWarnings)
                Console.WriteLine($"Found following warning in the compilation: {diagnostic.GetMessage()}");
            return !hasErrors;
        }
    }
}
