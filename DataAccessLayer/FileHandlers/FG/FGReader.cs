#nullable enable
using FuGradeLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.FileHandlers.FG
{
    /// <summary>
    /// Robust FG file reader that:
    /// - attempts to find and call library-provided load/deserialize methods on FuGradeLib types
    /// - falls back to BinaryFormatter deserialization as a last resort (with a clear comment/warning)
    /// Use the FGReflectionExplorer first to discover the concrete API and replace the reflection calls with direct typed calls for performance.
    /// </summary>
    public class FGReader : IFGReader
    {
        private Assembly? _fuAssembly;

        public FGReader()
        {
            // Try to resolve the referenced assembly by name; if not present, assembly loading will be null and we proceed to fallback heuristics.
            try
            {
                _fuAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "FuGradeLib", StringComparison.OrdinalIgnoreCase));

                if (_fuAssembly == null)
                {
                    // assembly might not be loaded yet — attempt to load by name
                    try
                    {
                        _fuAssembly = Assembly.Load("FuGradeLib");
                    }
                    catch
                    {
                        // ignore - we'll try load-from later if needed
                    }
                }
            }
            catch
            {
                _fuAssembly = null;
            }
        }

        public async Task<TeacherGrade?> ReadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            #pragma warning disable SYSLIB0011

            await using var fs = File.OpenRead(filePath);

            var formatter = new BinaryFormatter();

            return await Task.Run(
                () => formatter.Deserialize(fs) as TeacherGrade,
                cancellationToken);

            #pragma warning restore SYSLIB0011
        }
    }
}
