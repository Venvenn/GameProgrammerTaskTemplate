
public struct ChessmanBounceData
{
    public float strength;
    public float currentHealth;
    public float maxHealth;

    public ChessmanBounceData(float strength, float maxHealth)
    {
        this.strength = strength;
        this.currentHealth = maxHealth;
        this.maxHealth = maxHealth;
    }
}
