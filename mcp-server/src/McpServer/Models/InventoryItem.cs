namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Objeto en el inventario del jugador
/// </summary>
public class InventoryItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }

    /// <summary>
    /// Categoría del objeto (e.g., "medicine", "pokeballs", "held-items", "battle-items")
    /// </summary>
    public string Category { get; set; } = string.Empty;
}
