using Core;
using Core.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Tagger.NewFolder
{
    public class MusicService : IMusicService
    {
        private IOptions<AppSettings> appSettings;
        private ISqlDataProvider dataProvider;

        public MusicService(
            IOptions<AppSettings> appSettings,
            ISqlDataProvider dataProvider
            ) {
            this.appSettings = appSettings;
            this.dataProvider = dataProvider;
        }

        public async Task SaveTrack(TagLib.File file)
        {
            await this.dataProvider.ExecuteProcAsync("[MusicTrackInsert]", new
            {
                Filename = file.Name,
                Title = file.Tag.Title,
                Artist = file.Tag.FirstArtist,
                Album = file.Tag.Album,
            }).ConfigureAwait(false);
        }
    }

    public interface IMusicService
    {
        public Task SaveTrack(TagLib.File file);
    }
}
