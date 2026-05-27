using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DvergerSecretDefenses.Common {
    internal static class CommonUtils {

        public static List<Character> GetCharactersInRange(Vector3 position, float range) {
            Collider[] objs_near = Physics.OverlapSphere(position, range);
            List<Character> characters = new List<Character>();

            foreach (var col in objs_near) {
                var chara = col.GetComponentInChildren<Character>();
                if (chara != null) { characters.Add(chara); }
            }

            return characters;
        }
    }
}
