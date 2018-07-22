using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using olewoo_interop;

namespace Org.Benf.OleWoo.Typelib
{
    internal class OWDispInterface : ITlibNode
    {
        private readonly string _name;
        private readonly TypeAttr _ta;
        private readonly ITypeInfo _ti;
        private readonly bool _topLevel;

        private readonly OWIDispatchMethods _methodChildren;
        private readonly OWIDispatchProperties _propChildren;

        public OWDispInterface(ITlibNode parent, ITypeInfo ti, TypeAttr ta, bool topLevel)
        {
            Parent = parent;
            _name = ti.GetName();
            _ta = ta;
            _ti = ti;
            _methodChildren = new OWIDispatchMethods(this);
            _propChildren = new OWIDispatchProperties(this);
            _topLevel = topLevel;
            _data = new IDLData(this);
        }

        public override string Name => (_topLevel ? "dispinterface " : "") + _name;

        public override string ShortName => _name;

        public override string ObjectName => $"{_name}#di";

        public override List<string> GetAttributes()
        {
            var lprops = new List<string> { "uuid(" + _ta.guid + ")" };
            var ta = new TypeAttr(_ti);
            if (ta.wMajorVerNum != 0 || ta.wMinorVerNum != 0)
            {
                lprops.Add($"version({ta.wMajorVerNum}.{ta.wMinorVerNum})");
            }
            OWCustData.GetCustData(_ti, ref lprops);
            var help = _ti.GetHelpDocumentationById(-1, out var context);
            AddHelpStringAndContext(lprops, help, context);

            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FAGGREGATABLE)) lprops.Add("aggregatable");
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FAPPOBJECT)) lprops.Add("appobject");
            // TYPEFLAG_FCANCREATE is not applicable to interfaces/dispinterfaces
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FCONTROL)) lprops.Add("control");
            // No IDL syntax for TYPEFLAG_FDISPATCHABLE -- it is computed
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FDUAL)) lprops.Add("dual");
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FHIDDEN)) lprops.Add("hidden");
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FLICENSED)) lprops.Add("licensed");
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FNONEXTENSIBLE)) lprops.Add("nonextensible");
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FOLEAUTOMATION)) lprops.Add("oleautomation");
            // Can't find IDL for TYPEFLAG_FPREDECLID?!?
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FPROXY)) lprops.Add("proxy");
            // Can't find IDL for TYPEFLAG_FREPLACEABLE?!?
            if (0 != (_ta.wTypeFlags & TypeAttr.TypeFlags.TYPEFLAG_FRESTRICTED)) lprops.Add("restricted");
            // Can't find IDL for TYPEFLAG_FREVERSEBIND?!?

            return lprops;
        }

        /* Don't show a dispinterface at top level, UNLESS the corresponding interface is not itself
         * at top level. 
         */
        public override bool DisplayAtTLBLevel(ICollection<string> interfaceNames)
        {
            return !(interfaceNames.Contains(ShortName));
        }

        public override int ImageIndex => (int)ImageIndices.idx_dispinterface; 

        public override ITlibNode Parent { get; }

        public override List<ITlibNode> GenChildren()
        {
            var res = new List<ITlibNode>();

            /*
            if (_ta.cVars > 0) {
                _propChildren = new OWIDispatchProperties(this);
                res.Add(_propChildren);
            }
            if (_ta.cFuncs > 0)
            {
                _methodChildren = new OWIDispatchMethods(this);
                res.Add(_methodChildren);
            }
            */

            // Dispinterface can only use one of two possible formats, never both
            if (_ta.cImplTypes > 0)
            {
                res.Add(new OWDispInterfaceInheritedInterfaces(this, _ti, _ta));
            }
            else
            {
                res.Add(_propChildren);
                res.Add(_methodChildren);
            }
            return res;
        }

        public List<ITlibNode> MethodChildren()
        {
            var res = new List<ITlibNode>();
            var nfuncs = _ta.cFuncs;
            for (var idx = 0; idx < nfuncs; ++idx)
            {
                var fd = new FuncDesc(_ti, idx);
//                if (0 == (fd.wFuncFlags & FuncDesc.FuncFlags.FUNCFLAG_FRESTRICTED))
                res.Add(new OWMethod(this, _ti, fd));
            }
            return res;
        }

        public List<ITlibNode> PropertyChildren()
        {
            var res = new List<ITlibNode>();
            for (var x = 0; x < _ta.cVars; ++x)
            {
                var vd = new VarDesc(_ti, x);
                res.Add(new OWDispProperty(this, _ti, vd));
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

            //if (_ta.cFuncs > 0 || _ta.cVars > 0)
            //{
                // Naughty, but rely on side effect of verifying children.
                using (new IDLHelperTab(ih))
                {
                    _propChildren.BuildIDLInto(ih);
                    _methodChildren.BuildIDLInto(ih);
                }
            //}
            ih.AppendLine("};");
            ExitElement();
        }
        public override void EnterElement()
        {
            foreach (var listener in Listeners)
            {
                listener.EnterDispInterface(this);
            }
        }

        public override void ExitElement()
        {
            foreach (var listener in Listeners)
            {
                listener.ExitDispInterface(this);
            }
        }
    }
}