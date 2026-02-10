using CycloneDDS.Schema;

namespace Fdp.Examples.NetworkDemo.Descriptors
{
    [DdsTopic("SST_WeaponState")]
    public partial struct WeaponStateTopic
    {
        public long EntityId;
        public long InstanceId;
        public float Azimuth;
        public float Elevation;
        public int Ammo;
        public byte Status;
    }
}
