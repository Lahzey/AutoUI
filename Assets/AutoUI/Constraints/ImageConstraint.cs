using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageConstraint : AutoUIConstraint
{
    private Image imageComponent;

    [SerializeField] private string image;

    protected override void Awake()
    {
        base.Awake();
        imageComponent = GetComponent<Image>();
        AddValueInput(image);
    }

    public override void Render(DataContext context)
    {
        try
        {
            object image = values[0].Evaluate(context);
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