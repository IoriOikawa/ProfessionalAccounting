using System;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Util
{
    /// <summary>
    ///     ��չ���ַ���ƥ��
    /// </summary>
    public static class ParseHelper
    {
        /// <summary>
        ///     ���Կհ׺�ע��
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">����ʽ</param>
        public static void TrimStartComment(this FacadeBase facade, ref string expr)
        {
            expr = expr.TrimStart();
            if (expr.Length <= 2 ||
                expr[0] != '/' ||
                expr[1] != '/')
                return;

            var index = expr.IndexOf(Environment.NewLine, 2, StringComparison.Ordinal);
            expr = expr.Substring(index + Environment.NewLine.Length);
            expr = expr.TrimStart();
        }

        /// <summary>
        ///     ƥ������ź������ַ���
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">����ʽ</param>
        /// <param name="allow">��������</param>
        /// <param name="predicate">�Ƿ���Ч</param>
        /// <returns>�ַ���</returns>
        public static string Token(this FacadeBase facade, ref string expr, bool allow = true, Func<string, bool> predicate = null)
        {
            expr = expr.TrimStart();
            if (expr.Length == 0)
                return null;

            if (allow)
                if (expr[0] == '\'' ||
                    expr[0] == '"')
                    return facade.Quoted(ref expr);

            var id = 1;
            while (id < expr.Length)
            {
                if (char.IsWhiteSpace(expr[id]))
                    break;

                id++;
            }

            var t = expr.Substring(0, id);
            if (!predicate?.Invoke(t) == true)
                return null;

            expr = expr.Substring(id);
            return t;
        }

        /// <summary>
        ///     ƥ���ѡ����
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">����ʽ</param>
        /// <returns>��</returns>
        public static double? Double(this FacadeBase facade, ref string expr)
        {
            var d = double.NaN;
            if (facade.Token(ref expr, false, t => double.TryParse(t, out d)) != null)
                return d;

            return null;
        }

        /// <summary>
        ///     ƥ����
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">����ʽ</param>
        /// <returns>��</returns>
        public static double DoubleF(this FacadeBase facade, ref string expr)
            => double.Parse(facade.Token(ref expr, false));

        /// <summary>
        ///     ƥ���ѡ�ķ��㳤���ַ���
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">����ʽ</param>
        /// <param name="opt">�ַ���</param>
        /// <returns>�Ƿ�ƥ��</returns>
        // ReSharper disable once UnusedParameter.Global
        public static bool Optional(this FacadeBase facade, ref string expr, string opt)
        {
            expr = expr.TrimStart();
            if (!expr.StartsWith(opt, StringComparison.Ordinal))
                return false;

            expr = expr.Substring(opt.Length);
            return true;
        }

        /// <summary>
        ///     ƥ������ŵ��ַ���
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">����ʽ</param>
        /// <param name="c">���ţ���Ϊ�ձ�ʾ���⣩</param>
        // ReSharper disable once UnusedParameter.Global
        public static string Quoted(this FacadeBase facade, ref string expr, char? c = null)
        {
            expr = expr.TrimStart();
            if (expr.Length < 1)
                return null;

            var ch = expr[0];
            if (c != null &&
                ch != c)
                return null;

            var id = 0;
            while (true)
            {
                id = expr.IndexOf(ch, id + 1);
                if (id < 0)
                    throw new ArgumentException("�﷨����", nameof(expr));

                if (id == expr.Length - 1)
                    break;
                if (expr[id + 1] != ch)
                    break;
            }

            var s = expr.Substring(0, id + 1);
            expr = expr.Substring(id + 1);
            return s.Dequotation();
        }

        /// <summary>
        ///     ƥ���ѡ��ð�ſ�ʼ�ļ���ƾ֤����ʽ
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">����ʽ</param>
        /// <returns>����ƾ֤����ʽ</returns>
        public static IQueryCompunded<IVoucherQueryAtom> OptColVouchers(this FacadeBase facade, ref string expr)
            => Optional(facade, ref expr, ":") ? facade.VoucherQuery(ref expr) : null;
    }
}