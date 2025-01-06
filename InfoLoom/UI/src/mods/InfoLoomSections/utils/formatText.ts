export function formatWords(text: string, forceUpper: boolean = false): string {
    // Insert space between lowercase followed by uppercase
    text = text.replace(/([a-z])([A-Z])/g, '$1 $2');

    if (forceUpper) {
        // Capitalize first letter and letters after spaces
        text = text.replace(/(^[a-z])|(\ [a-z])/g, match => match.toUpperCase());
    }

    return text;
}
