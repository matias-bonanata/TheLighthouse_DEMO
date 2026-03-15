using UnityEngine;
using System.Collections.Generic;

namespace FastStudios
{
    public partial class InteractionManager // KEYS
    {
        public List<Key> ObtainedKeys = new List<Key>();
        [HideInInspector] public GameObject ObtainedKeysParent;
        [HideInInspector] public bool hasObtainedKeys;

        HashSet<Key> GetMatchingKeys(Interactable inter)
        {
            var result = new HashSet<Key>();
            if (inter == null || !inter.UseConditions || inter.Conditions == null || inter.Conditions.items == null)
                return result;

            foreach (var item in inter.Conditions.items)
            {
                if (item == null || !item.UseKey) continue;

                switch (item.KeyCheckMethod)
                {
                    case KeyAccept.KeyName:
                        {
                            Key k = null;
                            foreach (Key itKey in ObtainedKeys)
                            {
                                if (itKey == null) continue;

                                if (itKey.KeyName == item.KeyName)
                                {
                                    k = itKey;
                                    break;
                                }
                            }

                            if (k != null) result.Add(k);
                            break;
                        }
                    case KeyAccept.SpecificKey:
                        {
                            if (item.SpecificKey != null && ObtainedKeys.Contains(item.SpecificKey))
                                result.Add(item.SpecificKey);
                            break;
                        }
                }
            }

            return result;
        }

        public List<(Key, string)> GetMissingKeys(Interactable interactable)
        {
            if (!interactable.UseConditions) return null;

            List<(Key, string)> necessaryKeys = new List<(Key, string)>();

            foreach (var item in interactable.Conditions.items)
            {
                if (!item.UseKey) continue;

                Key keyToAdd = item.SpecificKey;
                if (item.KeyCheckMethod == KeyAccept.KeyName)
                {
                    necessaryKeys.Add((null, item.KeyName));
                    continue;
                }

                necessaryKeys.Add((keyToAdd, null));
            }

            return necessaryKeys;
        }

        public string MissingKeysString(Interactable interactable)
        {
            List<(Key, string)> missingKeys = GetMissingKeys(interactable);
            List<LogicalJoin> joins = interactable.Conditions.joins;

            string result = "";

            for (int i = 0; i < missingKeys.Count; i++)
            {
                if (i == missingKeys.Count - 1)
                {
                    if (missingKeys[i].Item1 != null) result += $"{missingKeys[i].Item1.KeyName}";
                    else result += $"{missingKeys[i].Item2}";
                }
                else
                {
                    if (joins[i] == LogicalJoin.And)
                    {
                        if (missingKeys[i].Item1 != null) result += $"{missingKeys[i].Item1.KeyName}, ";
                        else result += $"{missingKeys[i].Item2}, ";
                    }
                    else if (joins[i] == LogicalJoin.Or)
                    {
                        if (missingKeys[i].Item1 != null) result += $"{missingKeys[i].Item1.KeyName} Or ";
                        else result += $"{missingKeys[i].Item2} Or ";
                    }
                }
            }

            return result;
        }

        void UseAndMaybeConsumeKeys(Interactable inter)
        {
            var keys = GetMatchingKeys(inter);
            if (keys == null || keys.Count == 0) return;

            foreach (var k in keys)
            {
                if (k.DestroyOnSpecificInteractable)
                {
                    if (k.SpecificInteractable != null && inter != k.SpecificInteractable && k.SpecificDestroyWhenInteractable == SpecifInteractable.Specific) continue;
                    else if (k.SpecificInteractable != null && !k.SpecificInteractablesList.Contains(inter) && k.SpecificDestroyWhenInteractable == SpecifInteractable.Specifics) continue;
                }

                try { k.OnUse?.Invoke(); } catch { }

                if (k.DestroyWhenUsed)
                {
                    if (ObtainedKeys.Contains(k)) ObtainedKeys.Remove(k);
                    var go = k.gameObject;
                    if (go != null) Destroy(go);
                }

                if (k.RemoveKeyWhenUsed)
                {
                    if (ObtainedKeys.Contains(k)) ObtainedKeys.Remove(k);
                }
            }

            if (ObtainedKeysParent != null && ObtainedKeysParent.transform.childCount == 0)
            {
                Destroy(ObtainedKeysParent);
                hasObtainedKeys = false;
            }
        }

        public void DestroyKey(Key keyToDestroy)
        {
            if (!ObtainedKeys.Contains(keyToDestroy))
            {
                Debug.LogWarning($"[PressE] Obtained Keys does not contain the key {ObtainedKeys}, and it's trying to destroy it");
                return;
            }

            ObtainedKeys.Remove(keyToDestroy);
            Destroy(keyToDestroy);
        }

    }
}