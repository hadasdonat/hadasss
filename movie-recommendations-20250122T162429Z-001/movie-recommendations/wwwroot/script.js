// function triggerUpload() {
//     document.getElementById('file-input').click();
// }

// function showMovie() {
//     document.getElementById('result-section').style.display = 'block';
// }

// async function handleFileUpload(event) {
//     const file = event.target.files[0];
//     if (file) {
//         // כאן ניתן להוסיף לוגיקה לשיגור הקובץ ל-Backend
//         console.log("File selected:", file.name);

// 		const formData = new FormData();
//           formData.append('image', file);

// 		  const response = await fetch('/movies/recommendations', {
//                 method: 'POST',
//                 body: formData
//             });
//          const result = await response.json();
// 		console.log(result);
		
//         // סימולציה של הצגת תוצאות
//         document.getElementById('upload-section').style.display = 'none';
//         document.getElementById('result-section').style.display = 'block';
//         document.getElementById('movie-name').textContent =result.title; // שם סרט קבוע לדוגמה
// 		document.getElementById('movie-link').href =result.link; 
		
//     }
// }

