// Глобальные переменные для Web Audio API, чтобы избежать повторной инициализации
let audioContext;
let analyser;
let dataArray;
let sourceNode; // Переименовано из source, чтобы не конфликтовать с переменными
let isAudioContextInitialized = false; // Флаг инициализации

/**
 * Инициализирует AudioContext и Analyser при необходимости.
 * Вызывается при первом взаимодействии с плеером (например, Play).
 * @param {HTMLAudioElement} audioElement - Элемент <audio>, к которому подключаемся.
 */
function initAudioContextIfNeeded(audioElement) {
    // Инициализируем только один раз
    if (!isAudioContextInitialized && !audioContext && audioElement) {
        try {
            // Создаем AudioContext
            audioContext = new (window.AudioContext || window.webkitAudioContext)();
            // Создаем Analyser для анализа частот
            analyser = audioContext.createAnalyser();
            analyser.fftSize = 128; // Количество "столбиков" эквалайзера (степень двойки, min 32)
            // Создаем массив для данных частот
            dataArray = new Uint8Array(analyser.frequencyBinCount);

            // Создаем источник звука из HTML элемента <audio>
            sourceNode = audioContext.createMediaElementSource(audioElement);
            // Подключаем источник к анализатору
            sourceNode.connect(analyser);
            // Подключаем анализатор к стандартному выходу звука (динамикам)
            analyser.connect(audioContext.destination);

            isAudioContextInitialized = true; // Устанавливаем флаг
            console.log("AudioContext успешно инициализирован.");
        } catch (e) {
            console.error("Ошибка инициализации AudioContext:", e);
            // Можно скрыть эквалайзер, если инициализация не удалась
            const canvas = document.getElementById('equalizerCanvas');
            if (canvas) canvas.style.display = 'none';
        }
    }
    // Если контекст уже создан, но был приостановлен (например, автовоспроизведением), возобновляем
    if (audioContext && audioContext.state === 'suspended') {
        audioContext.resume();
    }
}

/**
 * Рисует столбики эквалайзера на Canvas.
 * @param {HTMLCanvasElement} canvasElement - Элемент <canvas> для рисования.
 * @param {CanvasRenderingContext2D} ctx - 2D контекст элемента <canvas>.
 * @param {HTMLAudioElement} audioElement - Элемент <audio> (для проверки состояния paused).
 */
function drawEqualizer(canvasElement, ctx, audioElement) {
    // Прекращаем рисовать, если контекст не создан, плеер на паузе, или элементы не найдены
    if (!isAudioContextInitialized || !analyser || !dataArray || !ctx || !canvasElement || audioElement.paused) {
        // Очищаем canvas, если анимация должна остановиться
        if (ctx && canvasElement) {
            ctx.clearRect(0, 0, canvasElement.width, canvasElement.height);
        }
        // Если контекст был инициализирован, но плеер остановлен, все равно запросим следующий кадр,
        // чтобы canvas успел очиститься. Браузер оптимизирует это.
        if (isAudioContextInitialized) {
            requestAnimationFrame(() => drawEqualizer(canvasElement, ctx, audioElement));
        }
        return; // Выходим из функции
    }

    // Запрашиваем следующий кадр анимации
    requestAnimationFrame(() => drawEqualizer(canvasElement, ctx, audioElement));

    // Получаем данные о частотах в реальном времени
    analyser.getByteFrequencyData(dataArray);

    // Очищаем canvas перед рисованием нового кадра
    ctx.clearRect(0, 0, canvasElement.width, canvasElement.height);

    const barWidth = (canvasElement.width / dataArray.length) * 1.5; // Ширина столбика
    let barHeight;
    let x = 0; // Позиция X для рисования столбика

    // Рисуем каждый столбик
    for (let i = 0; i < dataArray.length; i++) {
        barHeight = dataArray[i] * 0.35; // Масштабируем высоту (подбирается)

        // Цвет столбика (пример градиента)
        const percent = i / dataArray.length;
        const red = 100 + barHeight; // Красный зависит от высоты
        const green = 200 * percent;   // Зеленый зависит от позиции
        const blue = 50;              // Синий постоянный
        ctx.fillStyle = `rgb(${Math.min(255, red)}, ${Math.min(255, green)}, ${blue})`;

        // Рисуем сам столбик (от низа вверх)
        ctx.fillRect(x, canvasElement.height - barHeight, barWidth, barHeight);

        // Сдвигаем X для следующего столбика
        x += barWidth + 1; // +1 для небольшого отступа между столбиками
    }
}


// --- Основной код, выполняющийся после загрузки DOM ---
document.addEventListener('DOMContentLoaded', function () {

    // --- Получаем элементы плеера ---
    const audio = document.getElementById('audio');
    const playPauseButton = document.getElementById('playPause');
    const progressContainer = document.querySelector('.progress-bar');
    const progress = document.getElementById('progress');
    const volumeControl = document.getElementById('volumeControl'); // Используем ID
    const songTitleElement = document.getElementById('song-title');
    const songArtistElement = document.getElementById('song-artist');
    const canvas = document.getElementById('equalizerCanvas');
    let ctx = null; // Инициализируем контекст canvas

    // Проверяем наличие основных элементов плеера
    if (!audio || !playPauseButton || !progressContainer || !progress || !volumeControl || !songTitleElement || !songArtistElement) {
        console.error("Критическая ошибка: Не найдены все необходимые элементы аудиоплеера в DOM.");
        // Можно скрыть весь плеер, если он не может работать
        const playerElement = document.querySelector('.custom-audio-player');
        if (playerElement) playerElement.style.display = 'none';
        return; // Прекращаем выполнение дальнейшего кода плеера
    }

    // --- Настройка Canvas для эквалайзера ---
    if (canvas) {
        ctx = canvas.getContext('2d');
        const footerElement = document.querySelector('footer');
        if (footerElement && ctx) {
            // Устанавливаем размер canvas по размеру футера
            canvas.width = footerElement.offsetWidth;
            canvas.height = 50; // Фиксированная высота эквалайзера из CSS
        } else if (ctx) {
            // Запасной вариант, если футер не найден
            canvas.width = window.innerWidth;
            canvas.height = 50;
            console.warn("Footer element not found for canvas sizing.");
        } else {
            console.error("Не удалось получить 2D контекст для canvas эквалайзера.");
        }
    } else {
        console.warn("Canvas элемент для эквалайзера (#equalizerCanvas) не найден.");
    }


    // --- Обработчики событий плеера ---

    // 1. Кнопка Play/Pause
    playPauseButton.addEventListener('click', () => {
        // Инициализируем AudioContext при первом нажатии, если еще не сделано
        initAudioContextIfNeeded(audio);

        if (audio.paused) {
            // Пытаемся воспроизвести
            var playPromise = audio.play();
            if (playPromise !== undefined) {
                playPromise.then(_ => {
                    playPauseButton.textContent = '❚❚'; // Меняем иконку на паузу
                    if (isAudioContextInitialized && canvas && ctx) {
                        drawEqualizer(canvas, ctx, audio); // Начинаем рисовать эквалайзер
                    }
                }).catch(error => {
                    console.error("Ошибка воспроизведения аудио:", error);
                    // Здесь можно показать ошибку пользователю
                });
            }
        } else {
            audio.pause();
            playPauseButton.textContent = '▶'; // Меняем иконку на плей
        }
    });

    // 2. Обновление прогресс-бара во время воспроизведения
    audio.addEventListener('timeupdate', () => {
        // Проверяем, что длительность доступна и не равна 0 или бесконечности
        if (audio.duration && isFinite(audio.duration)) {
            const progressPercent = (audio.currentTime / audio.duration) * 100;
            progress.style.width = progressPercent + '%';
        } else {
            progress.style.width = '0%'; // Сбрасываем, если длительность некорректна
        }
    });

    // 3. Перемотка по клику на контейнер прогресс-бара
    progressContainer.addEventListener('click', function (event) {
        if (!audio.duration || !isFinite(audio.duration)) {
            console.warn("Перемотка невозможна: длительность аудио неизвестна.");
            return; // Не можем перемотать, если не знаем длительность
        }

        const rect = this.getBoundingClientRect(); // Размеры и позиция элемента
        const clickX = event.clientX - rect.left;  // Координата X клика внутри элемента
        const width = this.offsetWidth;           // Ширина элемента

        // Устанавливаем новое время, убедившись, что оно в пределах [0, duration]
        audio.currentTime = Math.max(0, Math.min(audio.duration, audio.duration * (clickX / width)));
    });

    // 4. Регулировка громкости
    volumeControl.addEventListener('input', (e) => {
        audio.volume = e.target.value;
    });

    // --- Обработчик для кнопок "Слушать" в каталоге (или других списках) ---
    // Находим ВСЕ кнопки с классом .play-song-btn на странице
    const playButtons = document.querySelectorAll('.play-song-btn');

    if (playButtons.length > 0) {
        playButtons.forEach(button => {
            button.addEventListener('click', function () {
                const songUrl = this.getAttribute('data-song-url'); // URL из data-атрибута
                const songItem = this.closest('.song-item'); // Находим родительский блок песни

                if (!songItem) {
                    console.error("Не найден родительский '.song-item' для кнопки 'Слушать'.");
                    return;
                }

                // Получаем полные названия из HTML
                const titleElement = songItem.querySelector('h3');
                const artistElement = songItem.querySelector('p:first-of-type'); // Первое <p> считаем исполнителем

                const fullTitle = titleElement ? titleElement.textContent : "Название неизвестно";
                const fullArtist = artistElement ? artistElement.textContent : "Исполнитель неизвестен";

                if (songUrl) {
                    console.log("Запуск трека:", songUrl);

                    // --- Обновляем информацию в плеере ---
                    // Устанавливаем полный текст (CSS его обрежет, если нужно)
                    songTitleElement.textContent = fullTitle;
                    songArtistElement.textContent = fullArtist;
                    // Устанавливаем атрибут title для всплывающей подсказки
                    songTitleElement.setAttribute('title', fullTitle);
                    songArtistElement.setAttribute('title', fullArtist);
                    // TODO: Обновить картинку плеера (song-cover), если есть URL картинки

                    // --- Запускаем воспроизведение ---
                    audio.src = songUrl; // Устанавливаем новый источник
                    // Инициализируем AudioContext перед первым воспроизведением через кнопку
                    initAudioContextIfNeeded(audio);

                    var playPromise = audio.play();
                    if (playPromise !== undefined) {
                        playPromise.then(_ => {
                            playPauseButton.textContent = '❚❚'; // Ставим значок паузы
                            if (isAudioContextInitialized && canvas && ctx) {
                                drawEqualizer(canvas, ctx, audio); // Запускаем эквалайзер
                            }
                        }).catch(error => {
                            console.error("Ошибка воспроизведения при клике на кнопку:", error);
                            playPauseButton.textContent = '▶'; // Возвращаем Play
                        });
                    } else {
                        // На случай, если .play() не возвращает Promise (старые браузеры)
                        playPauseButton.textContent = '❚❚';
                        if (isAudioContextInitialized && canvas && ctx) drawEqualizer(canvas, ctx, audio);
                    }

                } else {
                    console.error('URL песни (data-song-url) не найден для кнопки:', this);
                }
            });
        });
    } else {
        // Это нормально, если кнопок .play-song-btn нет на текущей странице (например, на главной)
        // console.log("Кнопки .play-song-btn не найдены на этой странице.");
    }

    

}); // --- Конец DOMContentLoaded ---