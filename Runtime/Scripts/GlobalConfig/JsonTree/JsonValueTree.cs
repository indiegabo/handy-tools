#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IndieGabo.HandyTools.GlobalConfigModule.JsonTree
{
    /// <summary>
    /// Hierarchical value tree backed by Newtonsoft.Json.
    /// </summary>
    public sealed class JsonValueTree : IValueTree
    {
        #region State

        private ValueNode _root;
        private readonly Dictionary<string, ValueNode> _index =
            new(StringComparer.Ordinal);

        #endregion

        #region Ctor

        public JsonValueTree()
        {
            _root = new ObjectNode(PathUtils.RootToken, null);
            RebuildIndex();
        }

        #endregion

        #region Load / Save

        public void LoadFromJson(string json)
        {
            json ??= "{}";
            var token = JToken.Parse(json);
            _root = FromToken(PathUtils.RootToken, null, token);
            _root.RecomputePathRecursive();
            RebuildIndex();
        }

        public string ToJson(bool indented = true)
        {
            var token = ToToken(_root);
            var formatting = indented ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(token, formatting);
        }

        #endregion

        #region IValueTree

        public bool TryGetNode(string path, out ValueNode node)
        {
            var key = PathUtils.Normalize(path);
            return _index.TryGetValue(key, out node!);
        }

        public bool TryGetValue<T>(string path, out T value)
        {
            value = default!;
            if (!TryGetNode(path, out var node)) return false;

            if (node is PrimitiveNode pn && pn.Value is T cast)
            {
                value = cast;
                return true;
            }

            if (node is PrimitiveNode pn2 && pn2.Value != null)
            {
                try
                {
                    var converted = (T)Convert.ChangeType(pn2.Value, typeof(T));
                    value = converted;
                    return true;
                }
                catch { }
            }

            return false;
        }

        public bool TryGetValue<T>(KeyPath<T> key, out T value) =>
            TryGetValue(key.Path, out value);

        public void SetValue(string path, object? value)
        {
            var key = PathUtils.Normalize(path);
            var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            if (_root is not ObjectNode rootObj)
                throw new InvalidOperationException("Root must be an object node.");

            ObjectNode current = rootObj;

            for (int i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                var isLast = i == parts.Length - 1;

                if (isLast)
                {
                    ValueNode newNode = value == null
                        ? new NullNode(part, current)
                        : new PrimitiveNode(part, current, value);
                    current.Set(part, newNode);
                    break;
                }

                if (!current.TryGet(part, out var next))
                {
                    var created = new ObjectNode(part, current);
                    current.Set(part, created);
                    current = created;
                    continue;
                }

                if (next is ObjectNode obj)
                {
                    current = obj;
                    continue;
                }

                var replacement = new ObjectNode(part, current);
                current.Set(part, replacement);
                current = replacement;
            }

            _root.RecomputePathRecursive();
            RebuildIndex();
        }

        public void SetValue<T>(KeyPath<T> key, T value) =>
            SetValue(key.Path, value);

        #endregion

        #region Index / Visit

        public void RebuildIndex()
        {
            _index.Clear();
            Visit(_root, n =>
            {
                if (!string.IsNullOrEmpty(n.Path))
                    _index[n.Path] = n;
            });
        }

        public void Visit(ValueNode node, Action<ValueNode> visitor)
        {
            visitor(node);
            switch (node)
            {
                case ObjectNode on:
                    foreach (var kv in on.Children)
                        Visit(kv.Value, visitor);
                    break;
                case ArrayNode an:
                    foreach (var item in an.Items)
                        Visit(item, visitor);
                    break;
            }
        }

        #endregion

        #region JToken Conversion

        private static ValueNode FromToken(
            string name, ValueNode? parent, JToken? token
        )
        {
            if (token is null) return new NullNode(name, parent);

            switch (token.Type)
            {
                case JTokenType.Object:
                    {
                        var obj = new ObjectNode(name, parent);
                        var jobj = (JObject)token;
                        foreach (var prop in jobj)
                        {
                            var child = FromToken(
                                prop.Key, obj, prop.Value ?? JValue.CreateNull()
                            );
                            obj.Set(prop.Key, child);
                        }
                        return obj;
                    }
                case JTokenType.Array:
                    {
                        var arr = new ArrayNode(name, parent);
                        var jarr = (JArray)token;
                        int i = 0;
                        foreach (var item in jarr)
                        {
                            var child = FromToken(
                                i.ToString(), arr, item ?? JValue.CreateNull()
                            );
                            arr.Add(child);
                            i++;
                        }
                        return arr;
                    }
                case JTokenType.Integer:
                    return new PrimitiveNode(name, parent, token.Value<long>());
                case JTokenType.Float:
                    return new PrimitiveNode(name, parent, token.Value<double>());
                case JTokenType.Boolean:
                    return new PrimitiveNode(name, parent, token.Value<bool>());
                case JTokenType.String:
                    return new PrimitiveNode(name, parent, token.Value<string>());
                case JTokenType.Null:
                case JTokenType.Undefined:
                default:
                    return new NullNode(name, parent);
            }
        }

        private static JToken ToToken(ValueNode node)
        {
            switch (node)
            {
                case ObjectNode on:
                    {
                        var o = new JObject();
                        foreach (var kv in on.Children)
                            o[kv.Key] = ToToken(kv.Value);
                        return o;
                    }
                case ArrayNode an:
                    {
                        var a = new JArray();
                        foreach (var item in an.Items)
                            a.Add(ToToken(item));
                        return a;
                    }
                case PrimitiveNode pn:
                    {
                        var v = pn.Value;
                        return v switch
                        {
                            null => JValue.CreateNull(),
                            string s => new JValue(s),
                            bool b => new JValue(b),
                            int i32 => new JValue(i32),
                            long i64 => new JValue(i64),
                            float f => new JValue((double)f),
                            double d => new JValue(d),
                            _ => new JValue(v.ToString())
                        };
                    }
                case NullNode:
                    return JValue.CreateNull();
                default:
                    return JValue.CreateNull();
            }
        }

        #endregion

        #region Accessors

        public ObjectNode RootObject =>
            _root as ObjectNode ??
            throw new InvalidOperationException("Root is not an object node.");

        #endregion
    }
}
