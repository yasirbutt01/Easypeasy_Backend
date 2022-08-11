using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EasyPeasy.DataViewModels.Enum
{
    public enum EPackage
    {
        Free1,
        Freelancer,
        Correspondent,
        Screenwriter,
        InkSlinger,
        EasyPeasyPremium,
        EasyPeasyScreenwriter,
        Ultimate
    }
    public static class Functions
    {
        public static Guid ToId(this EPackage value)
        {
            switch (value)
            {
                case EPackage.Free1:
                    return Guid.Parse("CD3FF55D-C84B-4434-9CC2-05D45AF8A2DB");
                case EPackage.Freelancer:
                    return Guid.Parse("E636DA73-5E7E-4638-847F-74A3609FA9D8");
                case EPackage.Correspondent:
                    return Guid.Parse("30DD6B37-AE84-4AB6-BB79-89A3E07BB378");
                case EPackage.Screenwriter:
                    return Guid.Parse("295AA771-CEB9-4A9E-BC60-E66C0679B590");
                case EPackage.InkSlinger:
                    return Guid.Parse("C5EA2E18-ED64-4BCB-AB27-98387BB9BDD8");
                case EPackage.EasyPeasyPremium:
                    return Guid.Parse("B997B8E2-A1ED-4F32-86DB-67E2F6953E3E");
                case EPackage.EasyPeasyScreenwriter:
                    return Guid.Parse("EB9502E8-DBAC-4A7D-BFCD-552F090D8EAE");
                case EPackage.Ultimate:
                    return Guid.Parse("5F5948C4-16D7-4557-B99D-E15676193295");
                default:
                    throw new InvalidDataException();
            }
        }
    }
}
