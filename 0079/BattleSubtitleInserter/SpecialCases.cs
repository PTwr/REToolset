using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleSubtitleInserter
{
    public static class SpecialCases
    {
        public static string OverrideAvatarIfNeeded(string voiceFile, string autodetectedAvatar)
        {
            if (VoiceFileToAvatar.TryGetValue(voiceFile, out var avatar)) return avatar;
            return autodetectedAvatar;
        }
        static SpecialCases()
        {
            //TR01
            Enumerable.Range(1, 25).Select(i => VoiceFileToAvatar[$"tut{i:D3}"] = "bng").ToList();
            //TR02
            Enumerable.Range(51, 82 - 51 + 1).Select(i => VoiceFileToAvatar[$"tut{i:D3}"] = "you").ToList();
            //TR03
            Enumerable.Range(101, 126 - 101 + 1).Select(i => VoiceFileToAvatar[$"tut{i:D3}"] = "guy").ToList();
        }
        public static Dictionary<string, string> VoiceFileToAvatar = new Dictionary<string, string>()
            {

                //AA01
                { "eva001", "amr" },
                { "eva002", "kai" },
                { "eva003", "hyt" },
                { "eva004", "chr" },
                { "eva005", "amr" },
                { "eva091", "chr" },
                //AA02
                { "eva006", "amr" },
                { "eva007", "kai" },
                //2nd cutscene
                { "eva009", "chr" },
                { "eva010", "amr" },
                { "eva011", "amr" },

                //ME01
                { "eve010", "aln" }, //intro
                //EVC_ST_006
                { "eve020", "hoa" },
                { "eve021", "aln" },
                { "eve022", "lls" },
                { "eve023", "dnb" },
                { "eve024", "aln" },
                { "eve025", "lls" },
                { "eve026", "aln" },
                { "eve543", "hoa" }, //reusable voice line?
                //EVC_ST_007 ending
                { "eve027", "aln" },
                { "eve028", "lls" },
                { "eve029", "hoa" },
                { "eve030", "lls" },
                { "eve031", "dnb" },
                { "eve032", "aln" },

                //ME05
                { "eve110", "aln" }, //first faceless voice

                //ME09
                //intro
                { "eve554", "hoa" },
                { "eve555", "aln" },
                //first part
                { "eve561", "hoa" },
                { "sle095", "hu2" }, //zeon
                //dom attack
                { "sle021", "hu2" },
                //EVC_ST_035
                { "eve558", "hoa" },
                { "eve559", "aln" },
                { "eve560", "hoa" },
                { "eve562", "aln" },
                //{ "sir017", "sir" },
                //EVC_ST_036
                { "eve563", "hoa" },
                { "eve564", "aln" },

                //ME15
                //EVE after EVC_ST_061 (third cutscene)
                //EVE has disabled LLN avatar for most voice lines :D
                { "eve595", "hoa" }, //or maybe Lily? listen moar
                { "eve596", "aln" },
                { "slb097", "hu1" }, //TODO show EFF or Zeon minion for traitor/assasin?
                { "eve597", "aln" },
                { "slb098", "hu1" }, //TODO show EFF or Zeon minion for traitor/assasin?
            };
    }
}
