# ShellEmulator

## 1. Общее описание

**ShellEmulator** — Windows Forms приложение, эмулирующее консоль с виртуальной файловой системой (VFS).
Поддерживает загрузку структуры из CSV и выполнение базовых команд: `ls`, `cd`, `vfs-cat`, `vfs-info` и др.
Опционально можно передать стартовый скрипт с командами.

Поддерживаемые аргументы командной строки:

```bash
--vfs <путь>      # загрузка CSV с виртуальной ФС
--script <путь>   # выполнение скрипта при старте
```

---

## 2. Описание функций и настроек

### Класс `Vfs`

Реализует виртуальную файловую систему.

**Основные свойства:**

* `Name` — имя VFS (имя файла CSV),
* `Sha256Hex` — хеш содержимого,
* `Root` — корневая нода (`/`).

**Ключевые методы:**

```csharp
ComputeSha256(byte[])      // Вычисление SHA-256
ParseCsv(string)           // Разбор CSV в структуру директорий/файлов
EnsureDirectory(string)    // Создание папок по пути
AddFile(string, byte[])    // Добавление файла
Resolve(string)            // Поиск узла VfsNode по пути
```

---

### Класс `VfsNode`

Модель файла или директории VFS.

**Свойства:**

```csharp
string Name
bool IsDirectory
byte[] Content
List<VfsNode> Children
```

---

### Класс `MainForm`

Главная форма приложения: UI, загрузка VFS, запуск скриптов, обработка команд.

**Аргументы конструктора:**

```csharp
MainForm(string? vfsPath, string? scriptPath)
```

**Основные команды:**

| Команда           | Описание                           |
| ----------------- | ---------------------------------- |
| `exit`            | Закрытие приложения                |
| `ls [путь]`       | Показать содержимое папки или файл |
| `cd <путь>`       | Перейти в директорию               |
| `vfs-info`        | Имя и SHA-256 текущего VFS         |
| `vfs-load <файл>` | Загрузка нового CSV                |
| `vfs-cat <путь>`  | Печать содержимого файла           |

**Пример логики обработки:**

```csharp
switch (cmd)
{
    case "exit":      ...
    case "ls":        CmdLs(args); break;
    case "cd":        CmdCd(args); break;
    case "vfs-info":  CmdVfsInfo(args); break;
    case "vfs-load":  CmdVfsLoad(args); break;
    case "vfs-cat":   CmdVfsCat(args); break;
    default:
        AppendOutput("Ошибка: неизвестная команда '" + cmd + "'");
        break;
}
```

---

## 3. Сборка и запуск

### Сборка проекта

```bash
dotnet build
```

### Запуск с аргументами

```bash
dotnet run -- --vfs ./data/vfs.csv --script ./init.txt
```

### Запуск без параметров

```bash
dotnet run
```

---

## 4. Примеры использования

### Запуск exe-файла

```bash
ShellEmulator.exe --vfs example.csv --script startup.txt
```

### Пример диалога в консоли приложения

```text
ls
cd dir/sub
vfs-cat readme.md
vfs-info
exit
```

### Загрузка нового VFS

```text
vfs-load newvfs.csv
```

### Пример файла скрипта (`startup.txt`)

```text
# коммент
vfs-load data.csv
cd docs
ls
vfs-cat info.txt
```
