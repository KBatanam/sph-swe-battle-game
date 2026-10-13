namespace SphSwe.Networking
{
    /// <summary>
    /// ネットワーク対戦におけるフィールド上の陣営。
    /// シミュレーションの Z 負方向側を Near、正方向側を Far とする。
    /// ホストが Near、ゲストが Far を担当する。
    /// </summary>
    public enum NetworkPlayerSide
    {
        Near = 0,
        Far = 1,
    }
}
