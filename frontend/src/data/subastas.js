
import { reactive } from 'vue'
import bici from '../assets/fotos/bici.jpg'
import camara from '../assets/fotos/camara.jpg'
import guitarra from '../assets/fotos/guitarra.jpg'
import iphone from '../assets/fotos/iphone.jpg'
import mecedora from '../assets/fotos/mecedora.jpg'
import ps5 from '../assets/fotos/ps5.jpg'
import reloj from '../assets/fotos/reloj.jpg'
import strat from '../assets/fotos/strat.jpg'
import nintendo from '../assets/fotos/switch.jpg'

const MIN = 60000
const ahora = Date.now()
const en = minutos => new Date(ahora + minutos * MIN).toISOString()
const hace = minutos => new Date(ahora - minutos * MIN).toISOString()
const serie = (desde, paso, n) => Array.from({ length: n }, (_, i) => desde + i * paso)

export const usuarioActual = { id: 1, nombre: 'María', apellido: 'Rodríguez', iniciales: 'MR', alias: 'maria.r', desde: 2024 }

export const vendedores = {
  luis: { id: 2, nombre: 'Luis Rosario', iniciales: 'LR', calificacion: 4.9, ventas: 32, ubicacion: 'Naco, Santo Domingo', zona: 'Naco', verificado: true, responde: 'Suele responder en menos de 1 hora' },
  ana: { id: 3, nombre: 'Ana Peña', iniciales: 'AP', calificacion: 4.7, ventas: 12, ubicacion: 'Gazcue, Santo Domingo', zona: 'Gazcue', verificado: true, responde: 'Suele responder el mismo día' },
}


export const categorias = [
  { id: 1, nombre: 'Celulares', icono: 'Smartphone' },
  { id: 2, nombre: 'Electrónica', icono: 'Monitor' },
  { id: 3, nombre: 'Videojuegos', icono: 'Gamepad2' },
  { id: 4, nombre: 'Instrumentos', icono: 'Music' },
  { id: 5, nombre: 'Hogar y muebles', icono: 'House' },
  { id: 6, nombre: 'Deportes', icono: 'Bike' },
  { id: 7, nombre: 'Relojes', icono: 'Watch' },
  { id: 8, nombre: 'Fotografía', icono: 'Camera' },
  { id: 9, nombre: 'Coleccionables', icono: 'Gem', soloBarra: true },
]

export const subcategorias = {
  Fotografía: ['Cámaras de rollo', 'Cámaras digitales', 'Lentes', 'Accesorios'],
  Instrumentos: ['Guitarras acústicas', 'Guitarras eléctricas', 'Teclados', 'Percusión'],
  Celulares: ['iPhone', 'Android', 'Accesorios'],
  Videojuegos: ['Consolas', 'Juegos', 'Controles'],
  Electrónica: ['Audio', 'Monitores', 'Computadoras'],
  'Hogar y muebles': ['Muebles', 'Decoración', 'Cocina'],
  Deportes: ['Bicicletas', 'Gimnasio', 'Pesca'],
  Relojes: ['Digitales', 'Análogos', 'Inteligentes'],
}

function pujas(montos, postores, inicioMin, pasoMin = 7) {
  return montos.map((monto, i) => ({ id: i + 1, postor: postores[i % postores.length], monto, creadaEn: hace(inicioMin - i * pasoMin) }))
}

const otros = ['jesus.p', 'carlo91', 'a.ruiz_rd', 'pedro.m']

export const subastas = reactive([
  {
    id: 10471, foto: ps5, fotos: [ps5], categoria: 'Videojuegos', subcategoria: 'Consolas', condicion: 'Usado, como nuevo', vendedor: vendedores.ana,
    titulo: 'PlayStation 5 con control DualSense y 2 juegos', precioSalida: 15000, incrementoMinimo: 300,
    fechaCierre: en(38), pujas: pujas(serie(15000, 300, 30).concat([24500]), otros, 600),
  },
  {
    id: 10477, foto: reloj, fotos: [reloj], categoria: 'Relojes', subcategoria: 'Digitales', condicion: 'Usado, buen estado', vendedor: vendedores.ana,
    titulo: 'Reloj Casio G-Shock GW-M5610U solar', precioSalida: 2500, incrementoMinimo: 250,
    fechaCierre: en(52), pujas: pujas([2500, 3000, 3250, 3500, 3750, 4000, 4250, 4500, 4750], ['carlo91', 'maria.r', 'jesus.p'], 300),
  },
  {
    id: 10466, foto: nintendo, fotos: [nintendo], categoria: 'Videojuegos', subcategoria: 'Consolas', condicion: 'Usado, buen estado', vendedor: vendedores.ana,
    titulo: 'Nintendo Switch OLED con dock y estuche', precioSalida: 9000, incrementoMinimo: 300,
    fechaCierre: en(57), pujas: pujas(serie(9200, 300, 20), otros, 400),
  },
  {
    id: 10482, foto: guitarra, categoria: 'Instrumentos', subcategoria: 'Guitarras acústicas', condicion: 'Usado, buen estado', vendedor: vendedores.luis,
  
    fotos: [
      { src: guitarra, encaje: 'contain' },
      { src: guitarra, posicion: '33% 85%', zoom: 1.7 },
      { src: guitarra, posicion: '50% 0%', zoom: 2.5 },
      { src: guitarra, posicion: '50% 100%', zoom: 2.5 },
    ],
    titulo: 'Guitarra acústica Yamaha FG-331 con estuche', precioSalida: 4000, incrementoMinimo: 500, precioReserva: 8000,
    visitas: 1284, siguiendo: 46, extensionFinal: true,
    descripcion: [
      'Guitarra acústica Yamaha FG-331 de los años 90. Suena muy bien, el mástil está derecho y los trastes están parejos. Tiene marcas normales de uso y un rayón pequeño en el borde inferior (se ve en la foto 4).',
      "Le cambié las cuerdas hace dos semanas (D'Addario calibre 12). Se entrega con estuche acolchado y correa.",
    ],
    descripcionCorta: 'Guitarra acústica Yamaha FG-331 de los años 90. Suena muy bien, el mástil está derecho y los trastes están parejos. Tiene un rayón pequeño en el borde inferior.',
    ficha: [['Estado', 'Usado, buen estado'], ['Marca y modelo', 'Yamaha FG-331'], ['Incluye', 'Estuche acolchado y correa'], ['Ubicación', 'Naco, Santo Domingo'], ['Entrega', 'Retiro en persona o envío (RD$ 350)']],
    preguntas: [{ pregunta: '¿Tiene algún problema en el mástil o en los clavijeros?', respuesta: 'No. El mástil está derecho y las clavijas afinan bien. Puedes venir a probarla antes de pujar.', respondidaEn: hace(180) }],
    fechaCierre: en(134),
    pujas: [
      ...pujas([4000, 4250, 4500, 4750, 5000, 5250, 5500, 5750, 6000, 6500, 7000, 7250], ['pedro.m', 'carlo91', 'jesus.p', 'a.ruiz_rd'], 2000, 50),
      { id: 13, postor: 'a.ruiz_rd', monto: 7500, creadaEn: hace(15 * 60 + 27) },
      { id: 14, postor: 'jesus.p', monto: 8000, creadaEn: hace(44) },
      { id: 15, postor: 'carlo91', monto: 8500, creadaEn: hace(21) },
      { id: 16, postor: 'jesus.p', monto: 9000, creadaEn: hace(3) },
      { id: 17, postor: 'maria.r', monto: 9500, creadaEn: hace(0.2) },
    ],
  },
  {
    id: 10455, foto: iphone, fotos: [iphone], categoria: 'Celulares', subcategoria: 'iPhone', condicion: 'Usado, buen estado', vendedor: vendedores.luis,
    titulo: 'iPhone 13 Pro 256 GB verde alpino, batería al 89 %', precioSalida: 20000, incrementoMinimo: 1000,
    fechaCierre: en(302), pujas: pujas(serie(19000, 500, 22).concat([30000, 31000]), ['pedro.m', 'jesus.p'].concat(Array(20).fill('pedro.m'), ['maria.r', 'jesus.p']), 1200),
  },
  {
    id: 10460, foto: camara, fotos: [camara], categoria: 'Fotografía', subcategoria: 'Cámaras de rollo', condicion: 'Usado, buen estado', vendedor: vendedores.luis,
    titulo: 'Cámara Canon AE-1 Program con lente 50 mm f/1.8', precioSalida: 6000, incrementoMinimo: 250,
    fechaCierre: en(30 * 60 + 6 * 60), pujas: pujas([6000, 6250, 6500, 6750, 7000, 7200], otros, 500),
  },
  {
    id: 10433, foto: bici, fotos: [bici], categoria: 'Deportes', subcategoria: 'Bicicletas', condicion: 'Usado, buen estado', vendedor: vendedores.ana,
    titulo: 'Bicicleta Trek 4500 aro 26, talla M', precioSalida: 8000, incrementoMinimo: 500,
    fechaCierre: en(43 * 60), pujas: pujas([8000, 8800, 9300, 9800, 10300, 10800, 11300, 11800, 12300, 13300, 13800], ['maria.r', 'jesus.p'], 2000),
  },
  {
    id: 10420, foto: mecedora, fotos: [mecedora], categoria: 'Hogar y muebles', subcategoria: 'Muebles', condicion: 'Restaurado', vendedor: vendedores.luis,
    titulo: 'Mecedora antigua de madera, restaurada', precioSalida: 4500, incrementoMinimo: 500,
    fechaCierre: en(51 * 60), pujas: pujas([4500, 5000, 5500, 6000], ['pedro.m', 'maria.r'], 3000),
  },
  {
    id: 10391, foto: strat, fotos: [strat], categoria: 'Instrumentos', subcategoria: 'Guitarras eléctricas', condicion: 'Usado, buen estado', vendedor: vendedores.ana,
    titulo: 'Fender Stratocaster Highway One', precioSalida: 30000, incrementoMinimo: 1000, oculta: true,
    fechaCierre: en(98 * 60), pujas: pujas([30000, 32000, 35000, 38000], ['jesus.p', 'maria.r'], 4000),
  },
  
  ...[
    [10350, 'Bocina JBL Flip 5 azul', 'Electrónica', 4200, true],
    [10344, 'Silla de oficina ergonómica con apoyo lumbar', 'Hogar y muebles', 8500, true],
    [10331, 'Samsung Galaxy S21 128 GB', 'Celulares', 17500, false],
    [10327, 'Monitor LG 27 pulgadas 4K', 'Electrónica', 12800, false],
    [10318, 'Xbox Series S 512 GB', 'Videojuegos', 15600, false],
    [10306, 'Teclado Yamaha PSR-E373', 'Instrumentos', 9200, false],
    [10298, 'Apple Watch Series 7 de 45 mm', 'Relojes', 11000, false],
  ].map(([id, titulo, categoria, final, gana], i) => ({
    id, titulo, categoria, foto: null, fotos: [], condicion: 'Usado, buen estado', vendedor: i % 2 ? vendedores.ana : vendedores.luis,
    precioSalida: Math.round(final * 0.6), incrementoMinimo: 250, oculta: true, fechaCierre: hace((i + 2) * 1440),
    pujas: [
      { id: 1, postor: gana ? 'pedro.m' : 'maria.r', monto: final - 500, creadaEn: hace((i + 2) * 1440 + 90) },
      { id: 2, postor: gana ? 'maria.r' : 'jesus.p', monto: final, creadaEn: hace((i + 2) * 1440 + 30) },
    ],
  })),
])


export const pujaActual = s => (s.pujas.length ? s.pujas[s.pujas.length - 1].monto : null)
export const montoMinimo = s => (s.pujas.length ? pujaActual(s) + s.incrementoMinimo : s.precioSalida)
export const voyGanando = s => s.pujas.length > 0 && s.pujas[s.pujas.length - 1].postor === usuarioActual.alias
export const buscarSubasta = id => subastas.find(s => s.id === Number(id))


export function registrarPuja(subasta, monto) {
  if (new Date(subasta.fechaCierre).getTime() <= Date.now()) return 'La subasta ya cerró.'
  if (subasta.vendedor.id === usuarioActual.id) return 'No puedes pujar en tu propia subasta.'
  if (!(monto >= montoMinimo(subasta))) return `La puja mínima es RD$ ${montoMinimo(subasta).toLocaleString('en-US')}.`
  subasta.pujas.push({ id: subasta.pujas.length + 1, postor: usuarioActual.alias, monto, creadaEn: new Date().toISOString() })
 
  if (subasta.extensionFinal && new Date(subasta.fechaCierre).getTime() - Date.now() < 2 * MIN) {
    subasta.fechaCierre = new Date(Date.now() + 2 * MIN).toISOString()
  }
  return null
}
