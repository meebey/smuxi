// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.Text;
using System.Runtime.InteropServices;

namespace PolarSSL
{
    [StructLayout(LayoutKind.Sequential)]
    public struct X509Buffer {
        //        int tag;                /**< ASN1 type, e.g. ASN1_UTF8_STRING. */
        public int Tag;
        //        size_t len;             /**< ASN1 length, e.g. in octets. */
        public UIntPtr DataLength;
        //        unsigned char *p;       /**< ASN1 data, e.g. in ASCII. */
        //public byte[] Data;
        public IntPtr DataPtr;

        public byte[] Data {
            get {
                var length = (int) DataLength;
                var ptr = DataPtr;
                if (ptr == IntPtr.Zero || length <= 0) {
                    return new byte[] {};
                }
                var array = new byte[length];
                Marshal.Copy(ptr, array, 0, length);
                return array;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct X509Name {
        //        x509_buf oid;               /**< The object identifier. */
        public X509Buffer OID;
        //        x509_buf val;               /**< The named value. */
        public X509Buffer Value;
        //        struct _x509_name *next;    /**< The next named information object. */
        public IntPtr NextPtr;

        public string DN {
            get {
                var buffer = new byte[1024];
                var dn = this;
                PolarSsl.x509parse_dn_gets(buffer, (UIntPtr) buffer.Length, ref dn);
                return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            }
        }

        public override string ToString()
        {
            return DN;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct X509Time {
        //        int year, mon, day;         /**< Date. */
        public int Year, Month, Day;
        //        int hour, min, sec;         /**< Time. */
        public int Hour, Minute, Second;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct X509Sequence {
        //        asn1_buf buf;                   /**< Buffer containing the given ASN.1 item. */
        public X509Buffer Buffer;
        //        struct _asn1_sequence *next;    /**< The next entry in the sequence. */
        public IntPtr Next;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MPI {
//        int s;              /*!<  integer sign      */
        public int Sign;
//        size_t n;           /*!<  total # of limbs  */
        public UIntPtr LimbsLength;
//        t_uint *p;          /*!<  pointer to limbs  */
        public IntPtr Limbs;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RsaContext {
//        int ver;                    /*!<  always 0          */
        public int Version;
//        size_t len;                 /*!<  size(N) in chars  */
        public UIntPtr Length;

//        mpi N;                      /*!<  public modulus    */
        public MPI N;
//        mpi E;                      /*!<  public exponent   */
        public MPI E;
//
//        mpi D;                      /*!<  private exponent  */
        public MPI D;
//        mpi P;                      /*!<  1st prime factor  */
        public MPI P;
//        mpi Q;                      /*!<  2nd prime factor  */
        public MPI Q;
//        mpi DP;                     /*!<  D % (P - 1)       */
        public MPI DP;
//        mpi DQ;                     /*!<  D % (Q - 1)       */
        public MPI DQ;
//        mpi QP;                     /*!<  1 / (Q % P)       */
        public MPI QP;
//
//        mpi RN;                     /*!<  cached R^2 mod N  */
        public MPI RN;
//        mpi RP;                     /*!<  cached R^2 mod P  */
        public MPI RP;
//        mpi RQ;                     /*!<  cached R^2 mod Q  */
        public MPI RQ;
//
//        int padding;                /*!<  RSA_PKCS_V15 for 1.5 padding and
//                                      RSA_PKCS_v21 for OAEP/PSS         */
        public int Padding;
//        int hash_id;                /*!<  Hash identifier of md_type_t as
//                                      specified in the md.h header file
//                                      for the EME-OAEP and EMSA-PSS
//                                      encoding                          */
        public int HashId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct X509Certificate {
        // x509_buf raw;               /**< The raw certificate data (DER). */
        public X509Buffer Raw;
        // x509_buf tbs;               /**< The raw certificate body (DER). The part that is To Be Signed. */
        public X509Buffer ToBeSigned;

        // int version;                /**< The X.509 version. (0=v1, 1=v2, 2=v3) */
        public int Version;
//        x509_buf serial;            /**< Unique id for certificate issued by a specific CA. */
        public X509Buffer Serial;
//        x509_buf sig_oid1;          /**< Signature algorithm, e.g. sha1RSA */
        public X509Buffer SignatureOID;

//        x509_buf issuer_raw;        /**< The raw issuer data (DER). Used for quick comparison. */
        public X509Buffer IsserRaw;
//        x509_buf subject_raw;       /**< The raw subject data (DER). Used for quick comparison. */
        public X509Buffer SubjectRaw;

//        x509_name issuer;           /**< The parsed issuer data (named information object). */
        public X509Name Issuer;
//        x509_name subject;          /**< The parsed subject data (named information object). */
        public X509Name Subject;

//        x509_time valid_from;       /**< Start time of certificate validity. */
        public X509Time ValidFrom;
//        x509_time valid_to;         /**< End time of certificate validity. */
        public X509Time ValidTo;

//        x509_buf pk_oid;            /**< Subject public key info. Includes the public key algorithm and the key itself. */
        public X509Buffer PublicKeyOID;
//        rsa_context rsa;            /**< Container for the RSA context. Only RSA is supported for public keys at this time. */
        public RsaContext Rsa;

//        x509_buf issuer_id;         /**< Optional X.509 v2/v3 issuer unique identifier. */
        public X509Buffer IsserId;
//        x509_buf subject_id;        /**< Optional X.509 v2/v3 subject unique identifier. */
        public X509Buffer SubjectId;
//        x509_buf v3_ext;            /**< Optional X.509 v3 extensions. Only Basic Contraints are supported at this time. */
        public X509Buffer Version3Extensions;
//        x509_sequence subject_alt_names;    /**< Optional list of Subject Alternative Names (Only dNSName supported). */
        public X509Sequence SubjectAlternativeNames;

//        int ext_types;              /**< Bit string containing detected and parsed extensions */
        public int ExtensionTypes;
//        int ca_istrue;              /**< Optional Basic Constraint extension value: 1 if this certificate belongs to a CA, 0 otherwise. */
        public int IsCA;
//        int max_pathlen;            /**< Optional Basic Constraint extension value: The maximum path length to the root certificate. Path length is 1 higher than RFC 5280 'meaning', so 1+ */
        public int MaximumPathLength;

//        unsigned char key_usage;    /**< Optional key usage extension value: See the values below */
        public byte KeyUsage;

//        x509_sequence ext_key_usage; /**< Optional list of extended key usage OIDs. */
        public X509Sequence ExtendedKeyUsage;

//        unsigned char ns_cert_type; /**< Optional Netscape certificate type extension value: See the values below */
        public byte NSCertificateType;

//        x509_buf sig_oid2;          /**< Signature algorithm. Must match sig_oid1. */
        public X509Buffer SignatureOID2;
//        x509_buf sig;               /**< Signature: hash of the tbs part signed with the private key. */
        public X509Buffer Signature;
//        int sig_alg;                /**< Internal representation of the signature algorithm, e.g. SIG_RSA_MD2 */
        public int SignatureAlgorithm;

//        struct _x509_cert *next;    /**< Next certificate in the CA-chain. */ 
        public IntPtr NextPtr;

        public X509Certificate Next {
            get {
                if (NextPtr == IntPtr.Zero) {
                    throw new IndexOutOfRangeException();
                }

                var nextStruct = (X509Certificate) Marshal.PtrToStructure(
                    NextPtr, typeof(X509Certificate)
                );
                return nextStruct;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct X509CertificateRevocationListEntry {
        //    x509_buf raw;
        public X509Buffer Raw;

        //    x509_buf serial;
        public X509Buffer Serial;

        //    x509_time revocation_date;
        public X509Time RevocationDate;

        //    x509_buf entry_ext;
        public X509Time EntryExt;

        //    struct _x509_crl_entry *next;
        public IntPtr NextPtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct X509CertificateRevocationList {
        //    x509_buf raw;           /**< The raw certificate data (DER). */
        public X509Buffer Raw;
        //    x509_buf tbs;           /**< The raw certificate body (DER). The part that is To Be Signed. */
        public X509Buffer ToBeSigned;

        //    int version;
        public int Version;
        //    x509_buf sig_oid1;
        public X509Buffer SignatureOID1;

        //    x509_buf issuer_raw;    /**< The raw issuer data (DER). */
        public X509Buffer IssuerRaw;

        //    x509_name issuer;       /**< The parsed issuer data (named information object). */
        public X509Name Issuer;

        //    x509_time this_update;  
        public X509Time ThisUpdate;
        //    x509_time next_update;
        public X509Time NextUpdate;

        //    x509_crl_entry entry;   /**< The CRL entries containing the certificate revocation times for this CA. */
        public X509CertificateRevocationListEntry Entry;

        //    x509_buf crl_ext;
        public X509Buffer CRLExt;

        //    x509_buf sig_oid2;
        public X509Buffer SignatureOID2;
        //    x509_buf sig;
        public X509Buffer Signature;
        //    int sig_alg;
        public int SignatureAlgorithm;

        //    struct _x509_crl *next; 
        public IntPtr Next;
    }

    public class PolarSsl
    {
        const string LIB_NAME = "libpolarssl.so.0";

        [DllImport(LIB_NAME)]
        // int x509parse_crt_der( x509_cert *chain, const unsigned char *buf, size_t buflen );
        public static extern int x509parse_crt_der(ref X509Certificate chain, byte[] buffer, UIntPtr bufferLength);

        [DllImport(LIB_NAME)]
        // int x509parse_crtfile( x509_cert *chain, const char *path );
        public static extern int x509parse_crtfile(ref X509Certificate chain, string path);

        [DllImport(LIB_NAME)]
        // int x509parse_dn_gets( char *buf, size_t size, const x509_name *dn )
        //internal static extern int x509parse_dn_gets(byte[] buffer, UIntPtr bufferLength, IntPtr dn); 
        internal static extern int x509parse_dn_gets(byte[] buffer, UIntPtr bufferLength, ref X509Name dn);

        [DllImport(LIB_NAME)]
//        int x509parse_verify(x509_cert *crt, x509_cert *trust_ca, x509_crl *ca_crl,
//                             const char *cn, int *flags,
//                             int (*f_vrfy)(void *, x509_cert *, int, int *),
//                             void *p_vrfy );
        public static extern int x509parse_verify(ref X509Certificate cert,
                                                  ref X509Certificate ca,
                                                  ref X509CertificateRevocationList crl,
                                                  string cn, ref int flags,
                                                  VerifyFunction f_vrfy,
                                                  IntPtr p_vrfy);
        public delegate int VerifyFunction(IntPtr verifyParameter, ref X509Certificate cert, int pathCount, ref int flags);
    }
}
