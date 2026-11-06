function resultados = analisisPulfrich2AFC(carpeta)
% ANALISISPULFRICH2AFC  Curvas psicométricas y PSE de la prueba Pulfrich 2AFC.
%
%   analisisPulfrich2AFC                        % abre un diálogo para elegir la carpeta
%   analisisPulfrich2AFC('Resultados/P01')      % carpeta de un participante
%   analisisPulfrich2AFC('.../P01_2AFC_xxx.csv') % o un CSV suelto
%   R = analisisPulfrich2AFC(...)               % devuelve también los resultados
%
% Lee todos los CSV *_2AFC_*.csv de la carpeta (así puedes partir la prueba
% en varias sesiones) y, para cada densidad óptica, ajusta por máxima
% verosimilitud una Gaussiana acumulada:
%
%     P("front right") = lapso + (1 - 2*lapso) * Phi( (x - PSE) / sigma )
%
% El PSE es el x donde el participante responde 50 % / 50 %, es decir, la
% compensación que anula el efecto Pulfrich de ese filtro. El intervalo de
% confianza del PSE sale por bootstrap (remuestreando dentro de cada nivel).
%
% Genera dos figuras (curvas por densidad y PSE frente a densidad) y, si
% cfg.guardar es true, las guarda con un resumen en <carpeta>/analisis.
%
% No necesita ninguna toolbox (ni Statistics ni Optimization).

%% ---------- Configuración ----------
cfg.variableX = 'retardo_ms';  % 'retardo_ms' (eje en ms) o 'comp_z' (unidades de Unity)
cfg.lapsoMax  = 0.06;          % lapso máximo por despistes (0 = sin lapsos)
cfg.nBoot     = 1000;          % remuestreos para el IC del PSE (0 = sin IC)
cfg.nivelIC   = 0.95;          % 0.68 si prefieres barras de ±1 error típico
cfg.trMax     = Inf;           % descarta ensayos con tiempo de reacción mayor (s)
cfg.guardar   = true;          % guarda figuras y resumen en <carpeta>/analisis
cfg.semilla   = 1;             % para que el bootstrap salga igual cada vez

%% ---------- Datos ----------
if nargin < 1 || isempty(carpeta)
    carpeta = uigetdir(pwd, 'Elige la carpeta del participante (Resultados/<id>)');
    if isequal(carpeta, 0), resultados = []; return; end
end

[datos, carpetaBase] = leerDatos(carpeta);

if ~isfield(datos, cfg.variableX) || all(isnan(datos.(cfg.variableX)))
    warning('La columna "%s" no existe o está vacía; se usa comp_z.', cfg.variableX);
    cfg.variableX = 'comp_z';
end
if strcmp(cfg.variableX, 'retardo_ms')
    etiquetaX = 'Retardo equivalente (ms)';
    unidad = 'ms';
else
    etiquetaX = 'Compensación Z (unidades Unity)';
    unidad = 'u';
end

x   = datos.(cfg.variableX);
r   = datos.front_right;
dop = datos.densidad_optica;
niv = datos.nivel;
validos = ~isnan(x) & (r == 0 | r == 1) & ~isnan(dop) & ...
          ~(datos.tiempo_reaccion_s > cfg.trMax);

if ~any(validos), error('No hay ensayos válidos.'); end
densidades = unique(dop(validos));
nD = numel(densidades);
idParticipante = datos.participante{1};

try
    rng(cfg.semilla);
catch
    rand('seed', cfg.semilla); %#ok<RAND>
end

%% ---------- Ajuste por densidad ----------
R = struct([]);
for k = 1:nD
    sel = validos & dop == densidades(k);
    xk = x(sel);  rk = r(sel);  nk = niv(sel);

    % Proporción observada por nivel (los puntos de la figura)
    [niveles, ~, g] = unique(nk);
    nNiv = numel(niveles);
    xObs = accumarray(g, xk, [nNiv 1], @mean);
    pObs = accumarray(g, rk, [nNiv 1], @mean);
    nObs = accumarray(g, 1,  [nNiv 1]);

    if all(rk == rk(1))
        warning('DO %+.2f: todas las respuestas son iguales; el PSE no es fiable.', densidades(k));
    end

    par = ajustarPsicometrica(xk, rk, cfg.lapsoMax, []);

    % Bootstrap estratificado: se remuestrea dentro de cada nivel
    ic = [NaN NaN];
    if cfg.nBoot > 0
        grupos = arrayfun(@(j) find(g == j), (1:nNiv)', 'UniformOutput', false);
        pseBoot = nan(cfg.nBoot, 1);
        for b = 1:cfg.nBoot
            ii = cell2mat(cellfun(@(v) v(randi(numel(v), numel(v), 1)), grupos, ...
                                  'UniformOutput', false));
            pb = ajustarPsicometrica(xk(ii), rk(ii), cfg.lapsoMax, par);
            pseBoot(b) = pb(1);
        end
        alfa = (1 - cfg.nivelIC) / 2;
        ic = [percentil(pseBoot, 100 * alfa), percentil(pseBoot, 100 * (1 - alfa))];
    end

    if par(1) < min(xk) || par(1) > max(xk)
        warning(['DO %+.2f: el PSE (%.2f) cae fuera de los niveles probados; ', ...
                 'amplía o desplaza los niveles en Unity.'], densidades(k), par(1));
    end

    R(k).densidad = densidades(k);
    R(k).n        = numel(rk);
    R(k).pse      = par(1);
    R(k).ic       = ic;
    R(k).sigma    = par(2);
    R(k).lapso    = par(3);
    R(k).niveles  = niveles;
    R(k).xObs     = xObs;
    R(k).pObs     = pObs;
    R(k).nObs     = nObs;
end

%% ---------- Resumen en la ventana de comandos ----------
fprintf('\nParticipante %s | eje X: %s | %d ensayos válidos\n', ...
        idParticipante, cfg.variableX, sum(validos));
for k = 1:nD
    fprintf('  DO %+5.2f  n=%4d  PSE = %7.3f %s  IC%2.0f%% [%7.3f, %7.3f]  sigma = %6.3f  lapso = %.3f\n', ...
            R(k).densidad, R(k).n, R(k).pse, unidad, 100 * cfg.nivelIC, ...
            R(k).ic(1), R(k).ic(2), abs(R(k).sigma), R(k).lapso);
end

pendiente = NaN;
if nD >= 2
    coef = polyfit([R.densidad], [R.pse], 1);
    pendiente = coef(1);
    fprintf('  Recta PSE = %.3f * DO + %.3f  (%s por unidad de densidad óptica)\n', ...
            coef(1), coef(2), unidad);
end
fprintf('\n');

%% ---------- Figura 1: curvas psicométricas (una por densidad) ----------
nCol = min(nD, 7);
nFil = ceil(nD / nCol);
fig1 = figure('Color', 'w', 'Name', 'Curvas psicometricas', ...
              'Position', [60 200 230 * nCol 300 * nFil]);

xMin = min(x(validos));  xMax = max(x(validos));
margen = 0.08 * max(xMax - xMin, eps);
xl = [xMin - margen, xMax + margen];
xs = linspace(xl(1), xl(2), 400);

for k = 1:nD
    subplot(nFil, nCol, k);
    hold on;
    curva = R(k).lapso + (1 - 2 * R(k).lapso) * fiNormal((xs - R(k).pse) / R(k).sigma);

    plot(xl, [0.5 0.5], '-', 'Color', [0.6 0.6 0.6]);
    plot([0 0], [0 1], '--', 'Color', [0.3 0.3 0.3]);
    plot(xs, curva, 'k-', 'LineWidth', 1.5);
    plot([R(k).pse R(k).pse], [0 0.5], 'k-');
    plot(R(k).pse, 0.03, 'kv', 'MarkerFaceColor', 'k', 'MarkerSize', 6);
    plot(R(k).xObs, R(k).pObs, 'ko', 'MarkerFaceColor', 'w', 'MarkerSize', 7, 'LineWidth', 1);

    xlim(xl);  ylim([0 1]);
    text(0.04, 0.97, sprintf('DO=%+.2f', R(k).densidad), 'Units', 'normalized', ...
         'VerticalAlignment', 'top');
    text(0.96, 0.06, sprintf('n=%d', R(k).n), 'Units', 'normalized', ...
         'HorizontalAlignment', 'right', 'VerticalAlignment', 'bottom');
    xlabel(etiquetaX);
    if mod(k - 1, nCol) == 0, ylabel('Proporción ''front right'''); end
    set(gca, 'TickDir', 'out', 'Box', 'off');
end

%% ---------- Figura 2: PSE frente a densidad óptica ----------
fig2 = [];
if nD >= 2
    fig2 = figure('Color', 'w', 'Name', 'PSE vs densidad', 'Position', [120 120 440 380]);
    hold on;
    dd = [R.densidad];
    pp = [R.pse];
    icM = vertcat(R.ic);

    xf = [min(dd), max(dd)] + [-0.1 0.1] * max(max(dd) - min(dd), eps);
    yTodos = [0, pp, icM(~isnan(icM))'];
    yl = [min(yTodos), max(yTodos)] + [-0.15 0.15] * max(max(yTodos) - min(yTodos), eps);
    plot(xf, [0 0], '-', 'Color', [0.75 0.75 0.75]);
    plot([0 0], yl, '-', 'Color', [0.75 0.75 0.75]);
    plot(xf, polyval(coef, xf), '-', 'Color', [0.45 0.45 0.45], 'LineWidth', 1.2);

    if all(~isnan(icM(:)))
        errorbar(dd, pp, pp - icM(:, 1)', icM(:, 2)' - pp, 'ko');
    end
    plot(dd, pp, 'ko', 'MarkerFaceColor', 'k', 'MarkerSize', 7);

    xlim(xf);  ylim(yl);
    xlabel('Densidad óptica  (- ojo izquierdo, + ojo derecho)');
    ylabel(sprintf('PSE (%s)', unidad));
    title(sprintf('%s  |  pendiente = %.2f %s por unidad de DO', idParticipante, pendiente, unidad));
    set(gca, 'TickDir', 'out', 'Box', 'off');
end

%% ---------- Guardar ----------
if cfg.guardar
    carpetaSalida = fullfile(carpetaBase, 'analisis');
    if ~exist(carpetaSalida, 'dir'), mkdir(carpetaSalida); end

    print(fig1, fullfile(carpetaSalida, [idParticipante '_curvas.png']), '-dpng', '-r200');
    if ~isempty(fig2)
        print(fig2, fullfile(carpetaSalida, [idParticipante '_PSE_vs_DO.png']), '-dpng', '-r200');
    end
    escribirResumen(fullfile(carpetaSalida, [idParticipante '_resumen_PSE.csv']), ...
                    R, idParticipante, cfg.variableX);
    fprintf('Figuras y resumen guardados en %s\n\n', carpetaSalida);
end

resultados.porDensidad = R;
resultados.pendiente   = pendiente;
resultados.cfg         = cfg;
resultados.datos       = datos;
end


%% ======================================================================
%%  Ajuste
%% ======================================================================

function par = ajustarPsicometrica(x, r, lapsoMax, par0)
% Máxima verosimilitud. Devuelve par = [PSE, sigma, lapso].
% sigma > 0 significa que "front right" aumenta al aumentar x.
x = x(:);  r = r(:);
if isempty(par0)
    par0 = inicialPorRejilla(x, r, lapsoMax);
end

% Se optimiza la pendiente 1/sigma (puede cruzar el 0 sin problema)
% y el lapso transformado para que quede siempre entre 0 y lapsoMax.
q0 = [par0(1), 1 / par0(2)];
if lapsoMax > 0
    lam0 = min(max(par0(3), 1e-3), 0.99 * lapsoMax);
    q0(3) = log(lam0 / (lapsoMax - lam0));
end

opts = optimset('Display', 'off', 'MaxFunEvals', 4000, 'MaxIter', 4000, ...
                'TolX', 1e-7, 'TolFun', 1e-9);
q = fminsearch(@(q) nllPsico(q, x, r, lapsoMax), q0, opts);

lam = 0;
if lapsoMax > 0, lam = lapsoMax / (1 + exp(-q(3))); end
par = [q(1), 1 / q(2), lam];
end

function nll = nllPsico(q, x, r, lapsoMax)
% Menos log-verosimilitud binomial de la Gaussiana acumulada con lapso
lam = 0;
if lapsoMax > 0, lam = lapsoMax / (1 + exp(-q(3))); end
p = lam + (1 - 2 * lam) * fiNormal(q(2) * (x - q(1)));
p = min(max(p, 1e-10), 1 - 1e-10);
nll = -sum(r .* log(p) + (1 - r) .* log(1 - p));
end

function par0 = inicialPorRejilla(x, r, lapsoMax)
% Punto de partida robusto: busca en una rejilla de PSE y sigma (con ambos signos)
rango = max(x) - min(x);
if rango <= 0, rango = 1; end
mus = linspace(min(x), max(x), 41);
sigmas = rango * logspace(-2, 0, 20);
lam = min(0.01, lapsoMax / 2);

mejor = Inf;
par0 = [mean(x), rango / 4, lam];
for s = [sigmas, -sigmas]
    for m = mus
        q = [m, 1 / s];
        if lapsoMax > 0, q(3) = log(lam / (lapsoMax - lam)); end
        v = nllPsico(q, x, r, lapsoMax);
        if v < mejor
            mejor = v;
            par0 = [m, s, lam];
        end
    end
end
end

function p = fiNormal(z)
% Función de distribución normal estándar sin Statistics Toolbox
p = 0.5 * erfc(-z / sqrt(2));
end

function v = percentil(x, p)
% Percentil con interpolación lineal (prctile necesita Statistics Toolbox)
x = sort(x(~isnan(x)));
n = numel(x);
if n == 0, v = NaN; return; end
pos = 1 + (n - 1) * p / 100;
v = x(floor(pos)) + (pos - floor(pos)) * (x(ceil(pos)) - x(floor(pos)));
end


%% ======================================================================
%%  Lectura de los CSV de Unity  (";" como separador y "," decimal)
%% ======================================================================

function [datos, carpetaBase] = leerDatos(ruta)
if exist(ruta, 'file') == 2
    archivos = {ruta};
    carpetaBase = fileparts(ruta);
    if isempty(carpetaBase), carpetaBase = pwd; end
else
    lista = dir(fullfile(ruta, '*_2AFC_*.csv'));
    if isempty(lista)
        error('No hay archivos *_2AFC_*.csv en %s', ruta);
    end
    archivos = cellfun(@(f) fullfile(ruta, f), {lista.name}, 'UniformOutput', false);
    carpetaBase = ruta;
end

datos = [];
for k = 1:numel(archivos)
    d = leerCsvPulfrich(archivos{k});
    if isempty(d), continue; end
    d.sesion = repmat(k, numel(d.ensayo), 1);
    datos = concatenar(datos, d, archivos{k});
    fprintf('Leído %s (%d ensayos)\n', archivos{k}, numel(d.ensayo));
end
if isempty(datos), error('Los archivos no tienen ensayos.'); end

necesarias = {'participante', 'ensayo', 'densidad_optica', 'nivel', 'front_right', 'tiempo_reaccion_s'};
for k = 1:numel(necesarias)
    if ~isfield(datos, necesarias{k})
        error('Falta la columna "%s". ¿Es un CSV de PruebaPulfrich2AFC?', necesarias{k});
    end
end
end

function d = leerCsvPulfrich(ruta)
% Devuelve un struct con un campo por columna: numérico si todas las
% celdas son números, cell de texto si no.
texto = fileread(ruta);
lineas = regexp(texto, '\r?\n', 'split');
lineas = lineas(~cellfun(@(s) isempty(strtrim(s)), lineas));

d = [];
if numel(lineas) < 2
    warning('%s no tiene ensayos; se ignora.', ruta);
    return;
end

cab = regexp(lineas{1}, ';', 'split');
% Quita el BOM de UTF-8 y cualquier carácter raro de los nombres de columna
cab = cellfun(@(s) regexprep(strtrim(s), '[^A-Za-z0-9_]', ''), cab, 'UniformOutput', false);
nC = numel(cab);
nF = numel(lineas) - 1;

celdas = repmat({''}, nF, nC);
for i = 1:nF
    v = regexp(lineas{i + 1}, ';', 'split');
    m = min(numel(v), nC);
    celdas(i, 1:m) = v(1:m);
end

for j = 1:nC
    col = strtrim(celdas(:, j));
    num = str2double(strrep(col, ',', '.'));
    esNumero = ~isnan(num) | cellfun(@isempty, col) | strcmpi(col, 'nan');
    if all(esNumero)
        d.(cab{j}) = num;
    else
        d.(cab{j}) = col;
    end
end
end

function a = concatenar(a, b, ruta)
if isempty(a), a = b; return; end
campos = fieldnames(b);
for i = 1:numel(campos)
    c = campos{i};
    if ~isfield(a, c)
        error('%s tiene una columna "%s" que no estaba en los anteriores.', ruta, c);
    end
    a.(c) = [a.(c); b.(c)];
end
end

function escribirResumen(ruta, R, idParticipante, variableX)
% Resumen en formato Excel español (";" y coma decimal)
fid = fopen(ruta, 'w');
if fid < 0, warning('No se pudo escribir %s', ruta); return; end
fprintf(fid, 'participante;variable_x;densidad_optica;n;pse;ic_inferior;ic_superior;sigma;lapso\n');
for k = 1:numel(R)
    fprintf(fid, '%s;%s;%s;%d;%s;%s;%s;%s;%s\n', idParticipante, variableX, ...
            num2coma(R(k).densidad), R(k).n, num2coma(R(k).pse), ...
            num2coma(R(k).ic(1)), num2coma(R(k).ic(2)), ...
            num2coma(abs(R(k).sigma)), num2coma(R(k).lapso));
end
fclose(fid);
end

function s = num2coma(v)
s = strrep(sprintf('%.4f', v), '.', ',');
end
