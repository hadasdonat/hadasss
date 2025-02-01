import sys
import cv2
from deepface import DeepFace
import json

def analyze_emotions(image_path):
    """
    Function to analyze emotions and gender from an image and return percentages for each emotion and detected gender.
    """
    # Load the image
    image = cv2.imread(image_path)
    if image is None:
        return json.dumps({"error": "Image not found at the specified path."})

    try:
        # Perform face and emotion analysis with gender detection
        analysis = DeepFace.analyze(image, actions=['emotion', 'gender'], enforce_detection=False)

    except Exception as e:
        return json.dumps({"error": f"Analysis failed: {str(e)}"})

    # Extract emotions and convert them to float percentages
    emotions = analysis[0]['emotion']  # Access the first result
    emotions = {emotion: float(value) for emotion, value in emotions.items()}  # Convert to float

    # Extract gender
    gender = analysis[0]['dominant_gender']

    # Combine emotions and gender into a single dictionary
    result = {
        "emotions": emotions,
        "gender": gender
    }

    # Return the combined result as JSON
    return json.dumps(result)

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(json.dumps({"error": "Usage: python emotion_analysis.py <image_path>"}))
        sys.exit(1)

    image_path = sys.argv[1]

    # Analyze the image and print the result
    result = analyze_emotions(image_path)
    print(result)
