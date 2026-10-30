<script setup>
// Ayuda: cómo funciona, reglas, fotos, pago y datos personales. Los enlaces del pie llevan a cada sección.
const secciones = [
  { id: 'pujar', titulo: 'Cómo pujar' },
  { id: 'reglas', titulo: 'Reglas de las subastas' },
  { id: 'vender', titulo: 'Vender y consejos para tus fotos' },
  { id: 'pago', titulo: 'Pago y entrega' },
  { id: 'privacidad', titulo: 'Tu cuenta y tus datos' },
]
</script>

<template>
  <div class="contenedor py-4 py-lg-5">
    <div class="row g-4">
      <nav class="col-lg-3 d-none d-lg-block" aria-label="Secciones de la ayuda">
        <div class="indice d-flex flex-column gap-2">
          <strong>Ayuda</strong>
          <a v-for="s in secciones" :key="s.id" :href="`#${s.id}`">{{ s.titulo }}</a>
        </div>
      </nav>

      <div class="col-lg-9 d-flex flex-column gap-4">
        <h1 class="fw-bold mb-0">Ayuda</h1>

        <section id="pujar" class="panel">
          <h2>Cómo pujar</h2>
          <ol>
            <li>Busca el artículo en el inicio o por categoría y abre la subasta.</li>
            <li>Escribe un monto igual o mayor que la <strong>puja mínima</strong> que aparece junto al precio.</li>
            <li>Confirma. Si vas ganando, lo verás enseguida; si alguien te supera, te avisamos por correo.</li>
          </ol>
          <p class="mb-0">
            <strong>Puja automática:</strong> escribe el máximo que estás dispuesto a pagar y el sistema puja por ti solo lo
            necesario para que vayas ganando, sin pasar de ese máximo. Puedes cambiarla o desactivarla mientras la subasta esté abierta.
          </p>
        </section>

        <section id="reglas" class="panel">
          <h2>Reglas de las subastas</h2>
          <ul class="mb-0">
            <li>La primera puja es igual o mayor que el precio de salida; las siguientes, la actual más el incremento mínimo.</li>
            <li>El vendedor no puede pujar en su propia subasta.</li>
            <li>No se puja antes del inicio ni después del cierre.</li>
            <li>Una puja en los últimos 2 minutos extiende el cierre 2 minutos, para que nadie gane en el último segundo.</li>
            <li>Si el vendedor puso un precio de reserva y la puja más alta no lo alcanza, la subasta queda desierta.</li>
            <li>Entre dos pujas automáticas gana la de máximo mayor; si empatan, la que se programó primero.</li>
            <li>Una puja no se puede retirar.</li>
          </ul>
        </section>

        <section id="vender" class="panel">
          <h2>Vender y consejos para tus fotos</h2>
          <p>
            Publicar es gratis y El Mejor Postor no cobra comisión. Eliges el precio de salida, el incremento mínimo, la duración
            (1, 3, 5 o 7 días) y, si quieres, un precio de reserva que los postores no ven.
          </p>
          <p>
            Puedes corregir o retirar tu subasta mientras nadie haya pujado. Con la primera puja ya no se puede cambiar, ni las fotos.
          </p>
          <p class="mb-2"><strong>Fotos:</strong> de 1 a 10, en JPG o PNG, hasta 10 MB cada una. La primera es la portada.</p>
          <ul class="mb-0">
            <li>Usa luz natural y un fondo liso.</li>
            <li>Muestra el artículo completo y desde varios ángulos.</li>
            <li>Fotografía de cerca las marcas, la etiqueta y cualquier detalle o desgaste: evita reclamos después.</li>
          </ul>
        </section>

        <section id="pago" class="panel">
          <h2>Pago y entrega</h2>
          <p>
            El Mejor Postor no cobra en línea. Al cerrar la subasta, el ganador y el vendedor ven el nombre, el correo y el
            teléfono del otro para ponerse de acuerdo en el pago y la entrega.
          </p>
          <ul class="mb-0">
            <li>El ganador tiene <strong>48 horas</strong> para pagar.</li>
            <li>Cuando recibe el pago, el vendedor lo confirma en «Mis ventas».</li>
            <li>
              Si el pago no se confirma a tiempo, la subasta pasa al siguiente postor que alcance la reserva, con 48 horas nuevas.
              Si no queda nadie, la subasta queda desierta.
            </li>
          </ul>
        </section>

        <section id="privacidad" class="panel">
          <h2>Tu cuenta y tus datos</h2>
          <ul class="mb-0">
            <li>Para crear la cuenta pedimos nombre, apellido, correo, cédula, teléfono y una contraseña.</li>
            <li>La contraseña se guarda cifrada; nadie puede leerla.</li>
            <li>En el historial de pujas los demás ven tu correo abreviado (por ejemplo, «m***a.r»), nunca completo.</li>
            <li>Tu nombre, correo y teléfono solo los ve la otra parte de una subasta que terminó con ganador, para coordinar el pago.</li>
            <li>La cédula no se muestra a otros usuarios. En «Mi perfil» la ves parcialmente oculta.</li>
            <li>Te escribimos solo por tus subastas (cuando te superan, al cerrar, por cambios de ganador y por el pago) y cuando pides recuperar la contraseña.</li>
          </ul>
        </section>
      </div>
    </div>
  </div>
</template>

<style scoped>
h2 {
  font-size: 20px;
  font-weight: 700;
  margin-bottom: 12px;
}

.indice {
  position: sticky;
  top: 96px;
}

li + li {
  margin-top: 6px;
}
</style>
