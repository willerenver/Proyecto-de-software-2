const numero = new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 })
const hora = new Intl.DateTimeFormat('es-DO', { hour: 'numeric', minute: '2-digit' })
const DIAS = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado']
const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre']
const MESES_CORTOS = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sept', 'oct', 'nov', 'dic']

// «RD$ 9,500», como en el diseño.
export function rd(monto) {
  return `RD$ ${numero.format(monto)}`
}

export function miles(monto) {
  return numero.format(monto)
}

// «38 min», «2 h 14 min», «1 d 6 h».
export function tiempoRestante(fecha, ahora = Date.now()) {
  const ms = new Date(fecha).getTime() - ahora
  if (ms <= 0) return 'Cerrada'
  const min = Math.floor(ms / 60000)
  const d = Math.floor(min / 1440)
  const h = Math.floor((min % 1440) / 60)
  const m = min % 60
  if (d > 0) return `${d} d ${h} h`
  if (h > 0) return `${h} h ${String(m).padStart(2, '0')} min`
  return `${m} min`
}

// «52 minutos», «2 horas», para frases completas.
export function tiempoEnPalabras(fecha, ahora = Date.now()) {
  const min = Math.max(0, Math.floor((new Date(fecha).getTime() - ahora) / 60000))
  if (min < 60) return `${min} ${min === 1 ? 'minuto' : 'minutos'}`
  const h = Math.floor(min / 60)
  if (h < 24) return `${h} ${h === 1 ? 'hora' : 'horas'}`
  const d = Math.floor(h / 24)
  return `${d} ${d === 1 ? 'día' : 'días'}`
}

export function cierraPronto(fecha, ahora = Date.now()) {
  const ms = new Date(fecha).getTime() - ahora
  return ms > 0 && ms < 60 * 60000
}

// «Sábado 24 de octubre, 8:00 p. m.»
export function fechaLarga(fecha) {
  const f = new Date(fecha)
  const dia = DIAS[f.getDay()]
  return `${dia[0].toUpperCase()}${dia.slice(1)} ${f.getDate()} de ${MESES[f.getMonth()]}, ${hora.format(f)}`
}

// «lunes 26 de octubre a las 8:00 p. m.»
export function fechaConHora(fecha) {
  const f = new Date(fecha)
  return `${DIAS[f.getDay()]} ${f.getDate()} de ${MESES[f.getMonth()]} a las ${hora.format(f)}`
}

// «Hoy, 10:42 a. m.», «Ayer, 7:15 p. m.» o «20 oct, 9:00 a. m.»
export function fechaRelativa(fecha, ahora = Date.now()) {
  const f = new Date(fecha)
  const hoy = new Date(ahora)
  const ayer = new Date(ahora - 86400000)
  const igual = (a, b) => a.toDateString() === b.toDateString()
  if (igual(f, hoy)) return `Hoy, ${hora.format(f)}`
  if (igual(f, ayer)) return `Ayer, ${hora.format(f)}`
  return `${f.getDate()} ${MESES_CORTOS[f.getMonth()]}, ${hora.format(f)}`
}

// «20 oct»
export function diaCorto(fecha) {
  const f = new Date(fecha)
  return `${f.getDate()} ${MESES_CORTOS[f.getMonth()]}`
}

// «hace 3 horas»
export function haceTiempo(fecha, ahora = Date.now()) {
  const min = Math.max(1, Math.floor((ahora - new Date(fecha).getTime()) / 60000))
  if (min < 60) return `hace ${min} ${min === 1 ? 'minuto' : 'minutos'}`
  const h = Math.floor(min / 60)
  if (h < 24) return `hace ${h} ${h === 1 ? 'hora' : 'horas'}`
  const d = Math.floor(h / 24)
  return `hace ${d} ${d === 1 ? 'día' : 'días'}`
}

// Valores de la columna articulo.condicion y cómo se muestran.
export const CONDICIONES = {
  nuevo: 'Nuevo',
  como_nuevo: 'Usado, como nuevo',
  buen_estado: 'Usado, buen estado',
  con_detalles: 'Usado, con detalles',
  restaurado: 'Restaurado',
}

export const condicion = valor => CONDICIONES[valor] ?? valor
