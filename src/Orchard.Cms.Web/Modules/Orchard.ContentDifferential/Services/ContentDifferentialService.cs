using System.Collections.Generic;
using System.Linq;

namespace Orchard.ContentDifferential.Services
{
    public class ContentDifferentialService : IContentDifferentialService
    {
        public string ApplyPatches(string text, IEnumerable<Patch> patches)
        {
            var diffMatchPatch = new diff_match_patch();

            return diffMatchPatch.patch_apply(patches.ToList(), text)[0] as string;
        }

        public IEnumerable<Patch> GeneratePatches(string text1, string text2)
        {
            var diffMatchPatch = new diff_match_patch();

            return diffMatchPatch.patch_make(text1, text2);
        }
    }
}
