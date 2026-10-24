# Надёжные HTTP-запросы в JavaScript и React

Учебная подборка примеров для безопасной работы с `fetch`: отмена запросов, тайм-ауты, обработка ошибок, защита от гонок и управление состояниями в React.

## Возможности

- обработка HTTP-ошибок `4xx/5xx`;
- отмена запросов через `AbortController`;
- тайм-ауты с помощью `AbortSignal.timeout`;
- объединение сигналов через `AbortSignal.any`;
- защита от гонок HTTP-запросов;
- паттерн «последний запрос побеждает»;
- React-хук `useFetch`;
- debounce для поисковых запросов;
- защита формы от двойной отправки;
- универсальная обёртка `safeFetch`.

## Почему возникают проблемы

При быстрых действиях пользователя запросы могут завершаться не в том порядке, в котором были отправлены.

Например:

```text
Запрос "a"   — 800 мс
Запрос "ab"  — 200 мс
Запрос "abc" — 400 мс
```

Ответ для `"a"` может прийти последним и заменить правильный результат для `"abc"`.

Для решения используются:

1. отмена предыдущего запроса;
2. идентификатор текущего запроса;
3. проверка актуальности результата перед обновлением интерфейса.

## Состояния запроса

| Состояние | Описание |
|---|---|
| `idle` | Запрос ещё не запускался |
| `loading` | Запрос выполняется |
| `success` | Данные успешно получены |
| `error` | Произошла ошибка |
| `aborted` | Запрос был отменён |

Отмена запроса обычно не считается ошибкой и не показывается пользователю.

## Проверка HTTP-ошибок

`fetch` не отклоняет Promise автоматически при статусах `404` или `500`. Поэтому нужно проверять `response.ok` вручную:

```js
const response = await fetch(url);

if (!response.ok) {
  throw new Error(`HTTP ${response.status}`);
}

const data = await response.json();
```

## Отмена запроса

```js
const controller = new AbortController();

fetch('/api/users', {
  signal: controller.signal
})
  .then(response => response.json())
  .then(data => console.log(data))
  .catch(error => {
    if (error.name === 'AbortError') {
      return;
    }

    console.error(error);
  });

controller.abort();
```

## Поиск без гонок

```js
let activeController = null;
let currentRequestId = 0;

async function search(query) {
  if (activeController) {
    activeController.abort();
  }

  const controller = new AbortController();
  activeController = controller;
  const requestId = ++currentRequestId;

  try {
    const response = await fetch(
      `/api/search?q=${encodeURIComponent(query)}`,
      { signal: controller.signal }
    );

    if (!response.ok) {
      throw new Error(`Сервер вернул ${response.status}`);
    }

    const data = await response.json();

    // Игнорируем устаревший ответ
    if (requestId !== currentRequestId) {
      return;
    }

    renderResults(data);
  } catch (error) {
    if (requestId !== currentRequestId) {
      return;
    }

    if (error.name === 'AbortError') {
      return;
    }

    renderError(error);
  }
}
```

## Тайм-аут

```js
async function fetchWithTimeout(url, timeout = 5000) {
  const response = await fetch(url, {
    signal: AbortSignal.timeout(timeout)
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }

  return response.json();
}
```

При тайм-ауте необходимо отдельно обрабатывать ошибку `TimeoutError`.

## React-хук `useFetch`

Хук хранит состояние запроса и автоматически отменяет старый запрос при:

- изменении URL;
- размонтировании компонента;
- запуске нового запроса.

Пример использования:

```jsx
function UserProfile({ userId }) {
  const { status, data, error } =
    useFetch(`/api/users/${userId}`);

  if (status === 'idle' || status === 'loading') {
    return <div>Загрузка...</div>;
  }

  if (status === 'error') {
    return <div>Ошибка: {error.message}</div>;
  }

  return (
    <div>
      <h1>{data.name}</h1>
      <p>{data.email}</p>
    </div>
  );
}
```

## Debounce для поиска

`debounce` откладывает выполнение функции, пока пользователь не перестанет печатать.

```js
const debouncedSearch = debounce(search, 300);

input.addEventListener('input', event => {
  debouncedSearch(event.target.value);
});
```

`debounce` уменьшает количество запросов, а `AbortController` отменяет уже запущенные устаревшие запросы.

## Защита от двойной отправки формы

При отправке формы кнопка блокируется до завершения запроса:

```js
if (isSubmitting) {
  return;
}

isSubmitting = true;
button.disabled = true;

try {
  const response = await fetch('/api/submit', {
    method: 'POST',
    body: formData
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }
} finally {
  isSubmitting = false;
  button.disabled = false;
}
```

## Важные замечания

- URL с переменными должны быть шаблонными строками: `` `/api/users/${userId}` ``.
- Ошибки HTTP нужно проверять через `response.ok`.
- `AbortError` обычно означает нормальную отмену запроса.
- `TimeoutError` означает превышение времени ожидания.
- Старые ответы нельзя передавать в интерфейс.
- `AbortController` отменяет ожидание на клиенте, но сервер может продолжить обработку запроса.
- `AbortSignal.timeout` и `AbortSignal.any` требуют современного браузера.
- Примеры в проекте являются отдельными сниппетами и не должны объединяться в один файл без дополнительной очистки.

## Итог

Надёжная работа с HTTP-запросами строится на четырёх правилах:

1. проверять `response.ok`;
2. отменять ненужные запросы;
3. игнорировать устаревшие ответы;
4. разделять состояния загрузки, успеха, ошибки и отмены.
