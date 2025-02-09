console.log("✅ קובץ ה-JavaScript נטען!");
       
function triggerUpload() {
    console.log("✅ `triggerUpload()` מופעל!");
    let input = document.getElementById("file-input");

    if (!input) {
        console.error("❌ אין `file-input`!");
        return;
    }

    console.log("📌 `file-input` נמצא, מסיר `display: none;` זמנית...");
    
    // 1️⃣ הופך את האלמנט לגלוי
    input.style.display = "block";  
    input.style.opacity = "0";  // שומר עליו מוסתר מבחינת המשתמש אבל פעיל
    
    // 2️⃣ מבצע קליק
    input.click();

    // 3️⃣ מחזיר אותו לנסתר אחרי שניה
    setTimeout(() => {
        input.style.display = "none";
    }, 100);
}
              
let emotionsResult = null; // משתנה גלובלי לאחסון תוצאות הרגשות
       
async function handleFileUpload(event) {
    console.log("✅ handleFileUpload() התחילה לעבוד"); // בדיקה שהפונקציה מופעלת

    const file = event.target.files[0];
    if (!file) return;

    console.log("✅ קובץ נבחר:", file.name); // בדיקה שהקובץ אכן נקלט

    const selectedGenres = Array.from(document.querySelectorAll('#genre-form input[name="genre"]:checked'))
        .map(checkbox => checkbox.value);

    // 🔹 הסתרת כל החלקים שאינם נחוצים והצגת הספינר
    document.getElementById('upload-section').style.display = 'none';
    document.getElementById('genre-selection').style.display = 'none';
    document.getElementById('loading-container').style.display = 'block';
    document.getElementById('loading-text').style.display = 'block';

    
    try {
        const formData = new FormData();
        formData.append('image', file);
        formData.append('topGeneresJson', JSON.stringify(selectedGenres));

        console.log("📡 שולח תמונה לשרת...");
        const emotionsResponse = await fetch('/movies/emotions', {
            method: 'POST',
            body: formData
        });

        console.log("📡 קיבלתי תשובה מהשרת!");
        emotionsResult = await emotionsResponse.json();
        console.log("📡 תגובת השרת:", emotionsResult);

        // 🔹 כיבוי הספינר והצגת התוצאות
        document.getElementById('loading-text').style.display = 'none';

        document.getElementById('loading-container').style.display = 'none';
        document.getElementById('emotions-section').style.display = 'block';
        
        // 🔹 עדכון מסך הביניים עם הרגשות שהגיעו מהשרת
        showEmotions(emotionsResult.emotions);

    } catch (error) {
        console.error("❌ Error:", error);
        alert("Failed to analyze image. Please try again.");

        // ❌ החזרת הממשק למצב ההתחלה אם יש כשל
        document.getElementById('loading-text').style.display = 'none';

        document.getElementById('loading-container').style.display = 'none';
        document.getElementById('emotions-section').style.display = 'none';
        document.getElementById('upload-section').style.display = 'block';
        document.getElementById('genre-selection').style.display = 'block';
    }
}    

function showEmotions(emotionsData) {
    if (!emotionsData || !emotionsData.emotions || !emotionsData.gender) {
        console.error("❌ Error: No valid emotions data received.");
        document.getElementById('emotions-container').innerHTML = "<p>❌ No emotions detected.</p>";
        return;
    }

    console.log("✅ showEmotions מופעל", emotionsData);

    const emotions = emotionsData.emotions;
    const gender = emotionsData.gender;

    // **מיון הרגשות כדי למצוא את שלושת הערכים הגבוהים ביותר**
    const sortedEmotions = Object.entries(emotions)
        .sort((a, b) => b[1] - a[1]) // ממיין מהגבוה לנמוך
        .slice(0, 3); // לוקח רק את שלושת הרגשות החזקים ביותר

    // **עדכון התצוגה**
    const container = document.getElementById('emotions-container');
    container.innerHTML = '<h3> Detected Emotions:</h3>';

    const emotionsRow = document.createElement('div');
    emotionsRow.className = 'emotions-row';

    sortedEmotions.forEach(([emotion, value]) => {  
        const percentage = (value ).toFixed(1); // 🔹 משאיר את האחוז המקורי **ללא הגבלת 100%**
        const emotionDiv = document.createElement('div');
        emotionDiv.className = 'emotion-item';
        emotionDiv.innerHTML = `
            <span class="emotion-name">${emotion.toUpperCase()}</span>
            <span class="emotion-value">${percentage}%</span>
        `;
        emotionsRow.appendChild(emotionDiv);
    });

    container.appendChild(emotionsRow);

    // **הצגת המגדר בנפרד**
    const genderText = document.createElement('p');
    genderText.className = "gender-text";
    genderText.innerHTML = `<strong>👤 Gender:</strong> ${gender}`;
    container.appendChild(genderText);

    console.log("✅ הצגת 3 הרגשות העיקריים והמגדר עם אחוזים אמיתיים!");
}

async function showMovieResult() {

    if (!emotionsResult || !emotionsResult.emotions) {
        console.error("🚨 Error: emotionsResult is not defined yet.");
        alert("Emotions data is not available yet. Please upload an image first.");
        return;
    }

    console.log("✅ Button clicked! Fetching recommended movie...");
    document.getElementById('emotions-section').style.display = 'none';
    document.getElementById('loading-container').style.display = 'block';
    document.getElementById('loading-text').style.display = 'block';


    const fileInput = document.getElementById('file-input');
    const file = fileInput.files[0];

    if (!file) {
        console.error("❌ No image file found in file-input.");
        alert("Error: No image found. Please upload a new image.");
        return;
    }

    const selectedGenres = Array.from(document.querySelectorAll('#genre-form input[name="genre"]:checked'))
        .map(checkbox => checkbox.value);

    console.log("📌 Debug: Selected genres:", selectedGenres);

    try {
        const formData = new FormData();
        formData.append('image', file);
        formData.append('emotions', JSON.stringify(emotionsResult.emotions));
        formData.append('gender', emotionsResult.gender || "");
        formData.append('genres', JSON.stringify(selectedGenres)); // 🆕 הוספת הז'אנרים

        console.log("📡 שולח בקשה לשרת עם הנתונים:", { file, emotions: emotionsResult.emotions, genres: selectedGenres });

        const response = await fetch('/movies/recommendations', {
            method: 'POST',
            body: formData
        });

        const responseText = await response.text(); // בדיקה שהתגובה תקינה
        console.log("📡 תגובת השרת בפורמט טקסט:", responseText);

        if (!response.ok) {
            throw new Error(`שגיאת שרת (${response.status}): ${responseText}`);
        }

        const result = JSON.parse(responseText);
        console.log("📡 JSON תקין מהשרת:", result);
        document.getElementById('loading-text').style.display = 'none';

        document.getElementById('loading-container').style.display = 'none';
        document.getElementById('result-section').style.display = 'block';

        // ניקוי התוצאות הקודמות
        document.getElementById('movie-card-1').style.display = 'none';
        document.getElementById('movie-card-2').style.display = 'none';

        if (result.length > 0 && result[0].title) {
            document.getElementById('movie-card-1').style.display = 'block';
            document.getElementById('movie-name-1').textContent = result[0].title;
            document.getElementById('movie-link-1').href = result[0].link;
        }

        if (result.length > 1 && result[1].title) {
            document.getElementById('movie-card-2').style.display = 'block';
            document.getElementById('movie-name-2').textContent = result[1].title;
            document.getElementById('movie-link-2').href = result[1].link;
        }

    } catch (error) {
        console.error("❌ Error fetching movie recommendation:", error);
        alert("Failed to get movie recommendation.");
        document.getElementById('loading-container').style.display = 'none';
        document.getElementById('loading-text').style.display = 'none';

    }
}

document.addEventListener("DOMContentLoaded", function() {
         console.log("✅ הסקריפט נטען לאחר שה-HTML סיים להיטען!");
        console.log("📌 האם emotions-section קיים?", document.getElementById("emotions-section"));
});

function restartPage() {
    console.log("🔄 Restarting page...");
    location.reload(); 
}
