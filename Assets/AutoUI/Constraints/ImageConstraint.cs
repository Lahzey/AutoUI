using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageConstraint : AutoUIConstraint
{
    private Image imageComponent;

    [SerializeField] private CodeInput imageConstraint;
    
    private Expression imageExpression;

    protected override void Awake()
    {
        base.Awake();
        imageComponent = GetComponent<Image>();
        
        ParseResult parseResult = imageConstraint.Result;
        imageExpression = parseResult is { Success: true } ? parseResult.Expression : null;
        if (imageExpression == null) Debug.LogError("Failed to parse image expression '" + imageConstraint.Input + "', defaulting to null.", this);
    }

    public override void Render(DataContext context)
    {
        try
        {
            object image = imageExpression?.Evaluate(context) ?? null;
            switch (image)
            {
                case null:
                    imageComponent.sprite = null;
                    break;
                case Sprite sprite:
                    imageComponent.sprite = sprite;
                    break;
                default:
                    Debug.LogError("ImageConstraint image is not a sprite: " + image, this);
                    break;
            }
        }
        catch (EvaluationException e)
        {
            Debug.LogException(e, this);
        }
    }
}