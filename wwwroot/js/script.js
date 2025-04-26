// Глобальные переменные для Web Audio API
let audioContext;
let analyser;
let dataArray;
let sourceNode;
let isAudioContextInitialized = false; // Флаг, что AudioContext создан

/**
 * Инициализирует AudioContext и Analyser при необходимости.
 * @param {HTMLAudioElement} audioElement - Элемент <audio>.
 */
function initAudioContextIfNeeded(audioElement) {
    if (!isAudioContextInitialized && !audioContext && audioElement) {
        try {
            audioContext = new (window.AudioContext || window.webkitAudioContext)();
            analyser = audioContext.createAnalyser();
            analyser.fftSize = 128;
            dataArray = new Uint8Array(analyser.frequencyBinCount);
            sourceNode = audioContext.createMediaElementSource(audioElement);
            sourceNode.connect(analyser);
            analyser.connect(audioContext.destination);
            isAudioContextInitialized = true;
            console.log("AudioContext успешно инициализирован.");
        } catch (e) {
            console.error("Ошибка инициализации AudioContext:", e);
            const canvas = document.getElementById('equalizerCanvas');
            if (canvas) canvas.style.display = 'none';
        }
    }
    // Возобновляем контекст, если он был приостановлен (важно для автовоспроизведения или после пауз)
    if (audioContext && audioContext.state === 'suspended') {
        audioContext.resume().catch(e => console.error("Ошибка возобновления AudioContext:", e));
    }
}

/**
 * Рисует столбики эквалайзера на Canvas.
 * @param {HTMLCanvasElement} canvasElement - Элемент <canvas>.
 * @param {CanvasRenderingContext2D} ctx - Контекст <canvas>.
 * @param {HTMLAudioElement} audioElement - Элемент <audio>.
 */
function drawEqualizer(canvasElement, ctx, audioElement) {
    // Проверяем все условия для рисования
    if (!isAudioContextInitialized || !analyser || !dataArray || !ctx || !canvasElement || audioElement.paused || audioElement.ended) {
        // Очищаем canvas, если анимация должна остановиться
        if (ctx && canvasElement) {
            ctx.clearRect(0, 0, canvasElement.width, canvasElement.height);
        }
        // Если плеер остановлен или закончил играть, больше не запрашиваем кадры
        return;
    }

    // Запрашиваем следующий кадр анимации *перед* отрисовкой текущего
    requestAnimationFrame(() => drawEqualizer(canvasElement, ctx, audioElement));

    // Получаем данные частот
    analyser.getByteFrequencyData(dataArray);

    // Очищаем canvas перед рисованием нового кадра
    ctx.clearRect(0, 0, canvasElement.width, canvasElement.height);

    const barWidth = (canvasElement.width / dataArray.length) * 1.5;
    let barHeight;
    let x = 0;

    // Рисуем столбики
    for (let i = 0; i < dataArray.length; i++) {
        barHeight = dataArray[i] * 0.35; // Масштаб
        const percent = i / dataArray.length;
        const red = 100 + barHeight;
        const green = 200 * percent;
        const blue = 50;
        ctx.fillStyle = `rgb(${Math.min(255, red)}, ${Math.min(255, green)}, ${blue})`;
        ctx.fillRect(x, canvasElement.height - barHeight, barWidth, barHeight);
        x += barWidth + 1;
    }
}

// --- Основной код ---
document.addEventListener('DOMContentLoaded', function () {

    // --- Получаем элементы плеера (один раз при загрузке) ---
    const audio = document.getElementById('audio');
    const playPauseButton = document.getElementById('playPause');
    const progressContainer = document.querySelector('.progress-bar');
    const progress = document.getElementById('progress');
    const volumeControl = document.getElementById('volumeControl');
    const songTitleElement = document.getElementById('song-title');
    const songArtistElement = document.getElementById('song-artist');
    const canvas = document.getElementById('equalizerCanvas');
    let ctx = null; // Контекст Canvas

    // Проверка наличия основных элементов
    if (!audio || !playPauseButton || !progressContainer || !progress || !volumeControl || !songTitleElement || !songArtistElement) {
        console.error("Критическая ошибка: Не найдены все необходимые элементы аудиоплеера в DOM.");
        const playerElement = document.querySelector('.custom-audio-player');
        if (playerElement) playerElement.style.display = 'none';
        return;
    }

    // --- Настройка Canvas ---
    if (canvas) {
        try {
            ctx = canvas.getContext('2d');
            const footerElement = document.querySelector('footer');
            if (footerElement && ctx) {
                canvas.width = footerElement.offsetWidth;
                canvas.height = 50; // Высота из CSS
            } else if (ctx) {
                canvas.width = window.innerWidth; canvas.height = 50;
            }
        } catch (e) {
            console.error("Не удалось получить 2D контекст для canvas эквалайзера:", e);
            if (canvas) canvas.style.display = 'none';
        }
    } else {
        console.warn("Canvas элемент для эквалайзера (#equalizerCanvas) не найден.");
    }

    // --- Обработчики событий плеера ---

    // 1. Кнопка Play/Pause в футере
    playPauseButton.addEventListener('click', () => {
        initAudioContextIfNeeded(audio); // Инициализируем/возобновляем контекст
        if (audio.paused) {
            if (!audio.src) { console.warn("Не выбран трек."); return; }
            var playPromise = audio.play();
            if (playPromise !== undefined) {
                playPromise.then(_ => {
                    playPauseButton.textContent = '❚❚';
                    // Запускаем отрисовку эквалайзера, если все готово
                    if (isAudioContextInitialized && canvas && ctx) {
                        drawEqualizer(canvas, ctx, audio);
                    }
                }).catch(error => console.error("Ошибка воспроизведения:", error));
            }
        } else {
            audio.pause();
            playPauseButton.textContent = '▶';
            // Отрисовка эквалайзера остановится сама в drawEqualizer
        }
    });

    // 2. Обновление прогресс-бара
    audio.addEventListener('timeupdate', () => {
        if (audio.duration && isFinite(audio.duration)) {
            progress.style.width = ((audio.currentTime / audio.duration) * 100) + '%';
        } else { progress.style.width = '0%'; }
    });

    // 3. Перемотка
    progressContainer.addEventListener('click', function (event) {
        if (!audio.duration || !isFinite(audio.duration)) return;
        const rect = this.getBoundingClientRect();
        audio.currentTime = Math.max(0, Math.min(audio.duration, audio.duration * ((event.clientX - rect.left) / this.offsetWidth)));
    });

    // 4. Громкость
    volumeControl.addEventListener('input', (e) => { audio.volume = e.target.value; });

    // 5. Окончание трека
    audio.addEventListener('ended', () => {
        playPauseButton.textContent = '▶';
        progress.style.width = '0%';
        audio.currentTime = 0;
        if (ctx && canvas) ctx.clearRect(0, 0, canvas.width, canvas.height); // Очистить canvas в конце
    });


    // --- Обработчик для ВСЕХ кнопок "Слушать" (.play-song-btn) ---
    function setupPlayButtonListeners() {
        const playButtons = document.querySelectorAll('.play-song-btn');
        // Получаем ссылки на элементы плеера один раз
        const audio = document.getElementById('audio');
        const songTitleElement = document.getElementById('song-title');
        const songArtistElement = document.getElementById('song-artist');
        const playPauseFooterButton = document.getElementById('playPause');
        const canvas = document.getElementById('equalizerCanvas'); // Нужен для drawEqualizer
        const ctx = canvas ? canvas.getContext('2d') : null;       // Нужен для drawEqualizer

        // Проверяем наличие критичных элементов плеера
        if (!audio || !songTitleElement || !songArtistElement || !playPauseFooterButton) {
            console.error("Не найдены основные элементы плеера в DOM для setupPlayButtonListeners.");
            return; // Нет смысла добавлять обработчики, если плеер не готов
        }

        if (playButtons.length === 0) return; // Нет кнопок - нечего делать

        playButtons.forEach(button => {
            if (button.dataset.listenerAttached === 'true') return; // Пропускаем, если уже добавили

            button.addEventListener('click', function () { // Используем function(), чтобы 'this' указывал на кнопку
                const songUrl = this.getAttribute('data-song-url');
                let fullTitle = "Название неизвестно";
                let fullArtist = "Исполнитель неизвестен";

                // --- ИЗМЕНЕНИЕ: Получение Названия и Исполнителя ---
                // Сначала пытаемся получить из data-атрибутов самой кнопки
                const dataTitle = this.getAttribute('data-title');
                const dataArtist = this.getAttribute('data-artist');

                if (dataTitle && dataArtist) { // Если атрибуты есть (кнопка у текста песни)
                    fullTitle = dataTitle;
                    fullArtist = dataArtist;
                    console.log("Получены данные из data-атрибутов кнопки.");
                } else {
                    // Иначе (если атрибутов нет), ищем родительский .song-item (старый способ)
                    const songItem = this.closest('.song-item');
                    if (songItem) {
                        const titleElement = songItem.querySelector('h3');
                        const artistElement = songItem.querySelector('p:first-of-type');
                        if (titleElement) fullTitle = titleElement.textContent;
                        if (artistElement) fullArtist = artistElement.textContent;
                        console.log("Получены данные из родительского .song-item.");
                    } else {
                        console.error("Не удалось найти ни data-атрибуты, ни родительский .song-item для кнопки:", this);
                        // Можно прервать выполнение или использовать значения по умолчанию
                    }
                }
                

                if (songUrl) {
                    console.log("Запуск трека:", songUrl, "Название:", fullTitle, "Исполнитель:", fullArtist);

                    // Обновляем информацию в плеере
                    songTitleElement.textContent = fullTitle;
                    songArtistElement.textContent = fullArtist;
                    songTitleElement.setAttribute('title', fullTitle);
                    songArtistElement.setAttribute('title', fullArtist);

                    // Устанавливаем трек и играем
                    audio.src = songUrl;
                    initAudioContextIfNeeded(audio); // Инициализация/возобновление контекста

                    var playPromise = audio.play();
                    if (playPromise !== undefined) {
                        playPromise.then(_ => {
                            playPauseButton.textContent = '❚❚';
                            if (isAudioContextInitialized && canvas && ctx) {
                                drawEqualizer(canvas, ctx, audio); // Запуск эквалайзера
                            }
                        }).catch(error => {
                            console.error("Ошибка воспроизведения:", error);
                            playPauseButton.textContent = '▶';
                        });
                    } else {
                        playPauseButton.textContent = '❚❚';
                        if (isAudioContextInitialized && canvas && ctx) drawEqualizer(canvas, ctx, audio);
                    }
                } else {
                    console.error('URL песни (data-song-url) не найден:', this);
                }
            });
            button.dataset.listenerAttached = 'true'; // Помечаем кнопку
        });
    }

    // Вызываем функцию установки обработчиков при загрузке страницы
    setupPlayButtonListeners();
}); 