#include <iostream>
#include <string>
#include <vector>

using namespace std;

struct TaskV1 {
    int id;
    string title;
    string date;
    bool completed;
};

struct TaskV2 {
    int id;
    string title;
    string date;
    bool completed;

    // Новые поля API v2
    int priority;
    string category;
};

void showTaskV1(const TaskV1& task) {
    cout << "\nAPI v1\n";
    cout << "ID: " << task.id << endl;
    cout << "Название: " << task.title << endl;
    cout << "Дата: " << task.date << endl;
    cout << "Выполнено: "
         << (task.completed ? "Да" : "Нет") << endl;
}

void showTaskV2(const TaskV2& task) {
    cout << "\nAPI v2\n";
    cout << "ID: " << task.id << endl;
    cout << "Название: " << task.title << endl;
    cout << "Дата: " << task.date << endl;
    cout << "Выполнено: "
         << (task.completed ? "Да" : "Нет") << endl;

    // Дополнительные поля v2
    cout << "Приоритет: " << task.priority << endl;
    cout << "Категория: " << task.category << endl;
}

int main() {
    TaskV1 task1 = {
        1,
        "Сделать лабораторную",
        "30.09.2026",
        false
    };

    TaskV2 task2 = {
        2,
        "Подготовить проект",
        "05.10.2026",
        false,
        1,
        "Учёба"
    };

    showTaskV1(task1);
    showTaskV2(task2);

    return 0;
}
