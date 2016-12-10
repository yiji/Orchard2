using System.Collections.Generic;

namespace Orchard.ContentDifferential.Services
{
    public interface IContentDifferentialService
    {
        string ApplyPatches(string text, IEnumerable<Patch> patches);

        IEnumerable<Patch> GeneratePatches(string text1, string text2);
    }
}
