namespace QuestDemonMR
{
    public static class RelicDropPolicy
    {
        public static (bool ammunition,bool health) Choose(int health,int totalAmmo,int killsAmmo,int killsLife,float dropRoll,float kindRoll)
        {
            var ammoPriority=totalAmmo<=18||killsAmmo>=4;
            var life=health>0&&health<100&&(killsLife>=4||dropRoll<.48f&&!ammoPriority&&kindRoll<.62f);
            return (ammoPriority||dropRoll<.48f&&!life,life);
        }
    }
}
