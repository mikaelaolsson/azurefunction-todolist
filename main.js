const api = "https://azuefunction-todolist.azurewebsites.net/api/";
const code = "?code={INSERTAZURECODEHERE}";

const todoList = document.getElementById('tasks-ol');
let template = document.querySelector("template");
const textbox = document.getElementById('task');
const addButton = document.getElementById('add-task');

const clearButton = document.getElementById('clear-list');
const clearPopup = document.getElementById('clear-list-popup');
const clearClose = document.getElementsByClassName("close")[0];
const clear = document.getElementById('clear');
const clearSelectList = document.querySelector('.wrapper select')

const historyPopup = document.getElementById("history-popup");
const historyClose = document.getElementsByClassName("close")[1];

const showAll = document.getElementById('all')
const showNotStarted = document.getElementById('not-started');
const showInProgress = document.getElementById('in-progress');
const showCompleted = document.getElementById('completed');


let filter = null;

async function loadTodos() {
    let url = api + "todos" + code;
    
    if (filter !== null) {
        url += "&status=" + filter;
    }

    const response = await fetch(url);
    todos = await response.json();

    todoList.replaceChildren();
    if (todos.length > 0) {
        todos.forEach(todo => {
            addTodo(todo); 
        });
    }
}

function addTodo(todo) {
    let li = template.content.firstElementChild.cloneNode(true);

    // Handle the status of the todo
    let select = li.querySelector('select');
    select.value = todo.status;
    select.classList.add(todo.status);

    select.onchange = async () => {
        await fetch(api + "todos" + code + "&id=" + todo.id, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                id: todo.id,
                status: select.value,
            }),
        });
        loadTodos();
    };

    // Handle the text of the todo
    let todoText = li.querySelector(".todo-text");
    todoText.textContent = todo.text;
    if (todo.status === "Completed") {
        todoText.style.textDecoration = "line-through";
        todoText.style.color = "#A09FA4"
    }

    // Handle the edit text option
    let edit = li.querySelector(".edit");
    let div = li.querySelector("#todo");

    div.ondblclick = () => {
        div.style.display = "none";
        edit.style.display = "block";
        edit.focus();
        edit.value = todoText.textContent;
    };

   edit.addEventListener("focusout", async function(){
        if (div.style.display === "none") {

            if (edit.value.trim() === todoText.textContent) {
                div.style.display = "block";
                edit.style.display = "none";
            }
            else if (edit.value.trim() !== "" && !(/^\s+$/.test(edit.value))) {
                todoText.textContent = edit.value.trim();
                div.style.display = "block";
                edit.style.display = "none";
                
                fetch (api + "todos" + code + "&id=" + todo.id, {
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        id: todo.id,
                        text: todoText.textContent
                    }),
                });
            }
            else {
                await fetch(api + "todos" + code + "&id=" + todo.id, {
                    method: "DELETE"
                });
                loadTodos();
            }
        }
    });

    // Handle the delete button
    let deleteButton = li.querySelector(".delete-button");
    deleteButton.onclick = async () => {
        await fetch (api + "todos" + code + "&id=" + todo.id, {
            method: "DELETE"
        });
        loadTodos();
    };

    // Handle the history button 
    let history = li.querySelector(".history-button");
    history.onclick = async () => {
        let historyWrap = document.querySelector(".history-wrap");
        removeAllChildren(historyWrap);

        const response = await fetch(api + "histories" + code + "&todoid=" + todo.id, {
            method: "GET",
        });
        let histories = await response.json();


        console.log(histories.length);

        let noHistory = document.querySelector("#no-history");

        if (histories.length > 0) {
            noHistory.style.display = "none";

            histories.forEach(history => {
                if (history.currentStatus !== history.oldStatus) {
                    let status = document.querySelector("#status-change");
                    let statusChange = status.content.firstElementChild.cloneNode(true);

                    let datetime = (history.edited).split(" ");
                    statusChange.querySelector(".edit-date").textContent = datetime[0];
                    statusChange.querySelector(".edit-time").textContent = datetime[1];

                    statusChange.querySelector(".old-status").textContent = (history.oldStatus).replace(/([a-z])([A-Z])/g, '$1 $2');
                    statusChange.querySelector(".old-status").classList.add(history.oldStatus);
                    statusChange.querySelector(".current-status").textContent = (history.currentStatus).replace(/([a-z])([A-Z])/g, '$1 $2');
                    statusChange.querySelector(".current-status").classList.add(history.currentStatus);
                    historyWrap.appendChild(statusChange);
                }
                else {
                    let text = document.querySelector("#text-change");
                    let textChange = text.content.firstElementChild.cloneNode(true);

                    let datetime = (history.edited).split(" ");
                    textChange.querySelector(".edit-date").textContent = datetime[0];
                    textChange.querySelector(".edit-time").textContent = datetime[1];

                    textChange.querySelector(".old-text").textContent = (history.oldText).replace(/([a-z])([A-Z])/g, '$1 $2');
                    textChange.querySelector(".current-text").textContent = (history.currentText).replace(/([a-z])([A-Z])/g, '$1 $2');


                    historyWrap.appendChild(textChange);
                }
            });
        }
        else {
            noHistory.style.display = "block";
        }
        historyPopup.style.display = "block";
    }
    todoList.appendChild(li);
}

textbox.addEventListener("keyup", function (event) {
    if (event.code === 'Enter') {
        event.preventDefault();
        addButton.click();
    }
});
addButton.onclick = async () => {
    if (textbox.value !== "" && !(/^\s+$/.test(textbox.value))) {
        await fetch(api + "todos" + code, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({text: (textbox.value).trim()}),
        });
        loadTodos();
    }
    textbox.value = "";
};

// Handle clear-all-button and closing of popups
clearButton.onclick = function() {
    let status = (location.hash).replace("#", "");
    status = status.replace("-", "");

    clearSelectList.value = status;
    clearPopup.style.display = "block";
}
clearClose.onclick = function() {
    clearPopup.style.display = "none";
}
historyClose.onclick = function() {
    historyPopup.style.display = "none";
}
window.onclick = function(event) {
  if (event.target == clearPopup) {
    clearPopup.style.display = "none";
  }
  if (event.target == historyPopup) {
    historyPopup.style.display = "none";
  }
}
clear.onclick = async () => {
    let url = api + "todos" + code;
    if (clearSelectList.value !== "") {
        url += "&status=" + clearSelectList.value;
    }

    await fetch(url, {
        method: "DELETE"
    });
    clearPopup.style.display = "none";
    loadTodos();
}
clearSelectList.onchange = () => {
    clearSelectList.className = "";
    clearSelectList.classList.add(clearSelectList.value);
}

function onChange() {
    getDate();
    var buttons = document.querySelectorAll(".filter-buttons button");
    buttons.forEach(button => {
        button.classList.remove("selected");
    });

    if (location.hash !== "") {
        filter = (location.hash).replace("#", "");
        console.log(filter);
        let id = filter.toLowerCase();
        document.getElementById(id).classList.add("selected");

        filter = filter.replace("-", "");
        console.log(filter)
        clearSelectList.classList.add(filter);
        loadTodos();
    }
    else {
        filter = null;
        showAll.classList.add("selected");
        loadTodos();
    }
}

window.onload = onChange;
window.onhashchange = onChange;

showAll.onclick = () => {
    location.hash = "";
}
showNotStarted.onclick = () => {
    location.hash = "Not-Started";
}
showInProgress.onclick = () => {
    location.hash = "In-Progress";
}
showCompleted.onclick = () => {
    location.hash = "Completed";
}

function getDate() {
    var months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    var dateObj = new Date();
    var month = months[dateObj.getUTCMonth()];
    var day = dateObj.getUTCDate();
    var year = dateObj.getUTCFullYear();

    var div = document.getElementById('title-date');
    var formatDate = month + " " + day + ", " + year;
    div.innerHTML = formatDate;
}

function removeAllChildren(parent) {
    while (parent.firstChild) {
        parent.removeChild(parent.firstChild);
    }
}