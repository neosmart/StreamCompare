using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamCompare
{
    public class FileCompare
    {
        public StreamCompare _comparer;

        public FileCompare()
        {
            _comparer = new StreamCompare();
        }

        public FileCompare(uint bufferSize)
        {
            _comparer = new StreamCompare((int) bufferSize);
        }

        public Task<bool> Compare(string path1, string path2)
        {
            return CompareAsync(path1, path2, CancellationToken.None);
        }

        public Task<bool> CompareAsync(string path1, string path2, CancellationToken cancel)
        {
            path1 = Path.GetFullPath(path1);
            path2 = Path.GetFullPath(path2);

            if (StringComparer.CurrentCulture.Compare(path1, path2) == 0)
            {
                return Task.FromResult(true);
            }

            var fileInfo1 = new FileInfo(path1);
            var fileInfo2 = new FileInfo(path2);

            if (fileInfo1.Length != fileInfo2.Length)
            {
                return Task.FromResult(false);
            }

            using (var stream1 = File.OpenRead(path1))
            using (var stream2 = File.OpenRead(path2))
            {
                return _comparer.CompareAsync(stream1, stream2, cancel, false);
            }
        }
    }
}
