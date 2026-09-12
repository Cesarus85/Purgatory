namespace QuestDemonMR
{
    public sealed class ArrivalSelection
    {
        private int _bats;
        public static bool WantsCeiling(int wave,int index)=>wave>=2&&(wave+index)%4==1;
        public PortalArrival Next(bool ceiling,int wave,int index)=>ceiling?
            (_bats++%2==0?PortalArrival.InvertedBurst:PortalArrival.Walk):
            ((wave+index)%2==0?PortalArrival.Leap:PortalArrival.Walk);
        public void Reset()=>_bats=0;
    }
}
