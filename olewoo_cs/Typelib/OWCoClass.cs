using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using olewoo_interop;
using IMPLTYPEFLAGS = System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS;

namespace Org.Benf.OleWoo.Typelib
{
    internal class OWCoClass : ITlibNode
    {
        private readonly string _name;
        private readonly TypeAttr _ta;
        private readonly ITypeInfo _ti;

        public OWCoClass(ITlibNode parent, ITypeInfo ti, TypeAttr ta)
        {
            Parent = parent;
            _name = ti.GetName();
            _ta = ta;
            _ti = ti;
            _data = new IDLData(this);
        }

        public override string Name => $"coclass {_name}";
        public override string ObjectName => $"{_name}#c";

        public override string ShortName => _name;
        public override List<string> GetAttributes()
        {
            var lprops = new List<string> { $"uuid({_ta.guid})" };
            var ta = new TypeAttr(_ti);
            if (_ta.wMajorVerNum != 0 || _ta.wMinorVerNum != 0)
            {
                lprops.Add($"version({ta.wMajorVerNum}.{ta.wMinorVerNum})");
            }
            OWCustData.GetCustData(_ti, ref lprops);
            var help = _ti.GetHelpDocumentationById(-1, out var context);
            AddHelpStringAndContext(lprops, help, context);

            if (0 == (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FCANCREATE)) lprops.Add("noncreatable");

            return lprops;
        }

        public override bool DisplayAtTLBLevel(ICollection<string> interfaceNames) => true;

        public override int ImageIndex => (int)ImageIndices.idx_coclass;
        public override ITlibNode Parent { get; }

        public override List<ITlibNode> GenChildren()
        {
            var res = new List<ITlibNode>();
            for (var x = 0; x < _ta.cImplTypes; ++x)
            {
                _ti.GetRefTypeOfImplType(x, out var href);
                _ti.GetRefTypeInfo(href, out var ti2);
                CommonBuildTlibNode(this, ti2, false, false, res);
            }
            return res;
        }

        public override void BuildIDLInto(IDLFormatter ih)
        {
            EnterElement();
            ih.AppendLine("[");
            var lprops = _data.Attributes;
            for (var i = 0; i < lprops.Count; ++i)
            {
                ih.AppendLine("  " + lprops[i] + (i < (lprops.Count - 1) ? "," : ""));
            }
            ih.AppendLine("]");
            ih.AppendLine(_data.Name + " {");
            using (new IDLHelperTab(ih))
            {
                for (var x = 0; x < _ta.cImplTypes; ++x)
                {
                    _ti.GetRefTypeOfImplType(x, out var href);
                    _ti.GetRefTypeInfo(href, out var ti2);
                    _ti.GetImplTypeFlags(x, out var itypflags);

                    var memInterface = new OWCoClassInterface(this, ti2, itypflags);
                    memInterface.BuildIDLInto(ih);
                }
            }
            ih.AppendLine("};");
            ExitElement();
        }
        public override void EnterElement()
        {
            foreach (var listener in Listeners)
            {
                listener.EnterCoClass(this);
            }
        }

        public override void ExitElement()
        {
            foreach (var listener in Listeners)
            {
                listener.ExitCoClass(this);
            }
        }
    }
}