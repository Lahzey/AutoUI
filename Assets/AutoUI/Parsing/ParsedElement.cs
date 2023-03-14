namespace AutoUI.Parsing {
/**
 * A class that represents any object that has been created to represent source code while parsing, even if that is not the final form of the object.
 */
public abstract class ParsedElement {
	public virtual string GetPlaceholder() {
		return ExpressionPatternAttribute.TypeOfPlaceholder(GetType());
	}
}
}