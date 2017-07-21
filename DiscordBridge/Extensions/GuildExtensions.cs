using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordBridge.Extensions
{
  public static class GuildExtensions
  {
    public static async Task<IEnumerable<IGuildChannel>> FindChannels(this IGuild guild, string match, bool exactMatch)
    {
      Func<IGuildChannel, bool> _matcher;

      if (exactMatch)
        _matcher = c => c.Name.Equals(match);
      else
        _matcher = c => c.Name.Equals(match, StringComparison.OrdinalIgnoreCase);

      return (await guild.GetChannelsAsync()).Where(_matcher);
    }
  }
}