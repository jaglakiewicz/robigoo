<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="UTF-8" indent="yes"/>

  <!-- ===== DATE HELPER: yyyy-MM-dd → dd.MM.yyyy ===== -->
  <xsl:template name="fmtDate">
    <xsl:param name="d"/>
    <xsl:choose>
      <xsl:when test="string-length($d) >= 10">
        <xsl:value-of select="substring($d,9,2)"/>.<xsl:value-of select="substring($d,6,2)"/>.<xsl:value-of select="substring($d,1,4)"/>
      </xsl:when>
      <xsl:otherwise><xsl:value-of select="$d"/></xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- ===== WADA / W NORMIE cells (two columns) ===== -->
  <!-- wada = defect found (false=fail), w normie = OK (true=pass) -->
  <xsl:template name="wn">
    <xsl:param name="v"/>
    <!-- wada cell -->
    <td class="chk">
      <xsl:if test="$v='false'">&#x25A0;</xsl:if>
      <xsl:if test="$v!='false'">&#x25A1;</xsl:if>
    </td>
    <!-- w normie cell -->
    <td class="chk">
      <xsl:if test="$v='true'">&#x25A0;</xsl:if>
      <xsl:if test="$v!='true'">&#x25A1;</xsl:if>
    </td>
  </xsl:template>

  <!-- Two wn pairs (for wyłączony + włączony napęd) -->
  <xsl:template name="wn2">
    <xsl:param name="v"/>
    <xsl:call-template name="wn"><xsl:with-param name="v" select="$v"/></xsl:call-template>
    <!-- second pair: always empty since model has no separate "running" check -->
    <td class="chk">&#x25A1;</td>
    <td class="chk">&#x25A1;</td>
  </xsl:template>

  <!-- checkbox (square) -->
  <xsl:template name="sq">
    <xsl:param name="on" select="false()"/>
    <xsl:choose>
      <xsl:when test="$on">&#x25A0;</xsl:when>
      <xsl:otherwise>&#x25A1;</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- ===== MAIN ===== -->
  <xsl:template match="/InspectionProtocol">
<html lang="pl">
<head>
<meta charset="UTF-8"/>
<title>Protokół badania technicznego nr <xsl:value-of select="ProtocolNumber"/></title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Times New Roman',Times,serif;font-size:7.5pt;color:#000;background:#bbb}
@media screen{
  .toolbar{position:fixed;top:0;left:0;right:0;z-index:999;background:#1c2340;padding:5px 14px;display:flex;align-items:center;gap:10px;box-shadow:0 2px 6px rgba(0,0,0,.5)}
  .btn{background:#d63444;color:#fff;border:none;padding:6px 16px;border-radius:3px;font-size:11px;font-weight:bold;cursor:pointer;font-family:Arial,sans-serif}
  .btn:hover{background:#b02232}
  .tlabel{color:#9ab;font-size:11px;font-family:Arial,sans-serif}
  body{padding-top:44px}
}
@media print{.toolbar{display:none}body{background:white;padding:0}}

/* PAGE */
.page{width:210mm;min-height:297mm;background:#fff;margin:6mm auto;padding:8mm 8mm 8mm 10mm;box-shadow:0 2px 10px rgba(0,0,0,.4);position:relative}
@media print{
  .page{width:100%;margin:0;padding:7mm 6mm 8mm 8mm;box-shadow:none;page-break-after:always}
  .page:last-child{page-break-after:auto}
}

/* TOP HEADER LAYOUT: left panel | right content */
.top-grid{display:grid;grid-template-columns:55mm 1fr;gap:3mm;margin-bottom:2mm}
.left-panel{border:1px solid #000;padding:2mm;font-size:7pt}
.left-panel .lsec{font-weight:bold;margin-bottom:1mm}
.left-panel .lfield{border-bottom:0.5px solid #555;min-height:12mm;margin-bottom:2mm;font-size:6.5pt}
.left-panel .lrow{display:flex;justify-content:space-between;align-items:baseline;margin-top:1mm;font-size:6.5pt}
.right-panel{}
.proto-title{font-size:10pt;font-weight:bold;text-align:center;margin-bottom:1.5mm}
.proto-subfield{font-size:8pt;margin:1mm 0}
.proto-nr{font-size:8pt;font-weight:bold;margin-bottom:2mm}

/* Owner box (right top-right) */
.posiadacz-box{border:1px solid #000;padding:2mm;font-size:7pt;margin-bottom:2mm}
.posiadacz-box .ptitle{font-weight:bold;margin-bottom:1mm}
.posiadacz-box .pfield{border-bottom:0.5px solid #555;min-height:9mm;margin-bottom:1.5mm;font-size:7pt;padding-bottom:1mm}
.posiadacz-box .psmall{font-size:6.5pt;margin-top:1mm}

/* Sprayer info box */
.sprayer-box{border:1px solid #000;padding:2mm;font-size:7.5pt}
.sprayer-box .srow{margin-bottom:1mm}
.sprayer-box .scb{font-size:9pt;margin-right:1mm}
.sprayer-box strong{font-weight:bold}

/* Summary block at top-right of page 1 */
.summary-block{font-size:8pt;margin-bottom:3mm}
.summary-block .sfield{margin-bottom:1.5mm}
.summary-block .sunderline{border-bottom:0.5px solid #000;display:inline-block;min-width:60mm}

/* MAIN INSPECTION TABLE */
.it{width:100%;border-collapse:collapse;font-size:6.5pt}
.it th,.it td{border:0.5px solid #000;padding:1px 2px;vertical-align:middle}
.it th{text-align:center;font-weight:bold;background:#f0f0f0;font-size:6pt}
.col-urz{width:20mm;font-weight:bold;font-size:6pt}
.col-rodz{width:18mm;font-size:6pt}
.col-pred{/* auto */}
.chk{width:7mm;text-align:center;font-size:9pt}
.col-uw{width:22mm;font-size:6pt}
.section-hdr td{background:#e0e0e0;font-weight:bold;text-align:center;font-size:7pt;padding:2px}
.sub-hdr td{background:#ebebeb;font-style:italic;font-size:6pt;padding:1px 2px}
.urz-cell{font-weight:bold;vertical-align:top;padding-top:2px}

/* Bottom tables */
.bttitle{font-weight:bold;text-align:center;font-size:7pt;background:#e8e8e8;border:0.5px solid #000;padding:2px}
.manotable,.nozzletable{width:100%;border-collapse:collapse;font-size:7pt;margin-top:0}
.manotable th,.manotable td,.nozzletable th,.nozzletable td{border:0.5px solid #000;padding:1.5px 3px;text-align:center}
.manotable th,.nozzletable th{background:#eee;font-size:6.5pt}
.nozzletable .nlbl{text-align:left;padding-left:3px;background:#f5f5f5;font-weight:bold;font-size:6.5pt}

/* Page 2 header continuation */
.cont-hdr{display:flex;justify-content:space-between;font-size:6.5pt;color:#555;margin-bottom:2mm;border-bottom:0.5px solid #ccc;padding-bottom:1mm}

/* RESULT section */
.result-bar{border:1px solid #000;padding:2mm;margin:3mm 0;font-size:7.5pt}
.result-line{margin:1mm 0}
.res-cb{font-size:10pt;margin-right:1mm}

/* Footnote */
.footnote{font-size:6pt;font-style:italic;color:#444;margin-top:3mm;border-top:0.5px solid #ccc;padding-top:1.5mm}
</style>
</head>
<body>

<!-- toolbar -->
<div class="toolbar">
  <button class="btn" onclick="window.print()">&#128424; Drukuj</button>
  <span class="tlabel">Protokół nr <xsl:value-of select="ProtocolNumber"/> — <xsl:value-of select="ClientName"/></span>
</div>

<!-- ============================================================ PAGE 1 ============================================================ -->
<div class="page" id="str1">

  <!-- TOP GRID: left = inspector info, right = protocol info + sprayer -->
  <div class="top-grid">

    <!-- LEFT PANEL: Podmiot przeprowadzający -->
    <div class="left-panel">
      <div class="lsec">Podmiot przeprowadzający badanie:</div>
      <div style="font-size:6.5pt;margin-bottom:0.5mm">Nr wpisu do rejestru:</div>
      <div class="lfield">&#160;<xsl:value-of select="InspectorLicenseNumber"/></div>
      <div style="font-size:6pt;color:#444;margin-bottom:1mm">Imię, nazwisko, miejsce zamieszkania, adres lub nazwa, siedziba i adres:</div>
      <div class="lfield">&#160;<xsl:value-of select="InspectorName"/></div>
      <div style="margin-top:8mm">
        <div class="lsec" style="margin-top:2mm">Pieczęć:</div>
        <div style="border:0.5px solid #777;height:20mm;width:100%">&#160;</div>
      </div>
      <div class="lrow" style="margin-top:3mm">
        <span>PESEL, NIP, inny*:</span>
        <span style="border-bottom:0.5px solid #555;min-width:20mm;display:inline-block">&#160;</span>
      </div>
      <div class="lrow" style="margin-top:2mm">
        <span><strong>Podpis posiadacza:</strong></span>
        <span style="border-bottom:0.5px solid #555;min-width:20mm;display:inline-block">&#160;</span>
      </div>
    </div>

    <!-- RIGHT PANEL -->
    <div class="right-panel">
      <div class="proto-title">Protokół badania technicznego nr: <span style="border-bottom:1px solid #000;display:inline-block;min-width:50mm"><xsl:value-of select="ProtocolNumber"/></span></div>

      <div style="font-size:7.5pt;margin-bottom:1mm">
        <strong>Miejsce badania</strong> (siedziba podmiotu lub inne (adres))*: <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:80mm"><xsl:value-of select="InspectionLocation"/></span>
      </div>

      <!-- Result summary at top right -->
      <div class="result-bar">
        <div class="result-line"><strong>Wynik badania:</strong></div>
        <div class="result-line">
          <span class="res-cb">
            <xsl:call-template name="sq"><xsl:with-param name="on" select="FinalResult='true'"/></xsl:call-template>
          </span> Pozytywny&#160;&#160;
          Nr znaku kontrolnego: <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:35mm"><xsl:value-of select="ControlStickerNumber"/></span>
        </div>
        <div class="result-line">
          <span class="res-cb">
            <xsl:call-template name="sq"><xsl:with-param name="on" select="FinalResult='false'"/></xsl:call-template>
          </span> Negatywny&#160;&#160;
          Powód: <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:55mm"><xsl:value-of select="GeneralNotes"/></span>
        </div>
        <div class="result-line" style="margin-top:1mm">
          <strong>Data przeprowadzenia badania:</strong>&#160;
          <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:25mm">
            <xsl:call-template name="fmtDate"><xsl:with-param name="d" select="InspectionDate"/></xsl:call-template>
          </span>
        </div>
        <div class="result-line">
          <strong>Termin ważności badania:</strong>&#160;
          <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:25mm">
            <xsl:call-template name="fmtDate"><xsl:with-param name="d" select="ValidUntil"/></xsl:call-template>
          </span>
        </div>
        <div class="result-line">
          <strong>Podpis diagnosty</strong> (osoby wykonującej badanie): <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:40mm">&#160;</span>
        </div>
      </div>

      <!-- Posiadacz (Owner) -->
      <div class="posiadacz-box">
        <div class="ptitle">Posiadacz sprzętu:</div>
        <div style="font-size:6.5pt;margin-bottom:0.5mm">Imię, nazwisko, miejsce zamieszkania i adres lub nazwa, siedziba i adres:</div>
        <div class="pfield"><xsl:value-of select="ClientName"/>&#160;<xsl:if test="ClientAddress != ''">, <xsl:value-of select="ClientAddress"/></xsl:if></div>
        <div class="psmall">PESEL, NIP, inny*: <span style="border-bottom:0.5px solid #555;display:inline-block;min-width:30mm"><xsl:value-of select="ClientTaxId"/></span></div>
        <div class="psmall" style="margin-top:1mm"><strong>Podpis posiadacza:</strong> <span style="border-bottom:0.5px solid #555;display:inline-block;min-width:30mm">&#160;</span></div>
      </div>

      <!-- Sprayer info (Opryskiwacz) -->
      <div class="sprayer-box">
        <div class="srow">
          <strong>Opryskiwacz</strong> (nazwa):
          <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:60mm"><xsl:value-of select="CropSprayerName"/></span>
        </div>
        <div class="srow">
          <strong>Nr seryjny</strong> lub ewidencyjny:
          <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:55mm"><xsl:value-of select="CropSprayerSerialNumber"/></span>
        </div>
        <div class="srow">
          <strong>Typ:</strong>&#160;
          polowy <span class="scb"><xsl:call-template name="sq"><xsl:with-param name="on" select="CropSprayerType='00'"/></xsl:call-template></span>
          sadowniczy <span class="scb"><xsl:call-template name="sq"><xsl:with-param name="on" select="CropSprayerType='01'"/></xsl:call-template></span>
          &#160;&#160;<strong>Pojemność zbiornika (l):</strong> <xsl:value-of select="TankCapacity"/>
        </div>
        <div class="srow">
          <strong>Rodzaj:</strong>&#160;
          zawieszany <span class="scb"><xsl:call-template name="sq"><xsl:with-param name="on" select="CropSprayerKind='00'"/></xsl:call-template></span>
          przyczepiany <span class="scb"><xsl:call-template name="sq"><xsl:with-param name="on" select="CropSprayerKind='01'"/></xsl:call-template></span>
          samobieżny <span class="scb"><xsl:call-template name="sq"><xsl:with-param name="on" select="CropSprayerKind='02'"/></xsl:call-template></span>
          inny <span class="scb"><xsl:call-template name="sq"><xsl:with-param name="on" select="CropSprayerKind='03'"/></xsl:call-template></span>
        </div>
        <div class="srow">
          <strong>Producent</strong>, rok produkcji: <xsl:value-of select="CropSprayerManufacturer"/><xsl:if test="CropSprayerProductionYear != ''">, <xsl:value-of select="CropSprayerProductionYear"/></xsl:if>
        </div>
        <div class="srow">
          <strong>Data zakupu / ostatniego badania*:</strong>
          <span style="border-bottom:0.5px solid #000;display:inline-block;min-width:40mm">&#160;</span>
        </div>
      </div>

    </div><!-- /right-panel -->
  </div><!-- /top-grid -->

  <!-- MAIN INSPECTION TABLE - Section 1 and beginning of Section 2 -->
  <table class="it">
    <thead>
      <tr>
        <th class="col-urz" rowspan="3">Urządzenie opryskiwacza</th>
        <th class="col-rodz" rowspan="3">Rodzaj wyposażenia</th>
        <th class="col-pred" rowspan="3">Przedmiot badań</th>
        <th colspan="2">Ocena przy wyłączonym napędzie</th>
        <th colspan="2">Ocena przy włączonym napędzie</th>
        <th class="col-uw" rowspan="3">Uwagi i zalecenia</th>
      </tr>
      <tr>
        <th class="chk">wada</th>
        <th class="chk">w normie</th>
        <th class="chk">wada</th>
        <th class="chk">w normie</th>
      </tr>
    </thead>
    <tbody>

      <!-- ======== 1. Badanie ogólne opryskiwacza ======== -->
      <tr class="section-hdr">
        <td colspan="8">1. Badanie ogólne opryskiwacza</td>
      </tr>
      <tr>
        <td class="col-urz" rowspan="5">&#160;</td>
        <td class="col-rodz" rowspan="5">&#160;</td>
        <td>1.1&#160;&#160;Kompletność, stan techniczny, osłony części wirujących</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="EquipmentCompletePassed"/></xsl:call-template>
        <td class="col-uw" rowspan="5"><xsl:value-of select="GeneralSectionNotes"/></td>
      </tr>
      <tr>
        <td>1.2&#160;&#160;Pewność mocowania w układzie zawieszenia</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="MarkingsReadablePassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>1.3&#160;&#160;Stan zużycia części – zespołów</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="GeneralConditionPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>1.4&#160;&#160;Szczelność zbiornika</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="TankSealingPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>1.5&#160;&#160;Czystość</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>

      <!-- ======== 2. Badanie stanu technicznego ======== -->
      <tr class="section-hdr">
        <td colspan="8">2 Badanie stanu technicznego poszczególnych części i urządzeń opryskiwacza</td>
      </tr>

      <!-- 2.1 Pompa -->
      <tr>
        <td class="urz-cell" rowspan="5"><strong>2.1 Pompa</strong><br/>Natężenie wypływu<br/>[dm³/min]<br/>............</td>
        <td class="col-rodz" rowspan="2">
          &#x25A1; tłokowa<br/>&#x25A1;&#160;<br/>membranova<br/>&#x25A1; inna<br/>typ..........
        </td>
        <td>2.1.1&#160;Szczelność</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="PumpSealingPassed"/></xsl:call-template>
        <td class="col-uw" rowspan="5"><xsl:value-of select="PumpSectionNotes"/></td>
      </tr>
      <tr>
        <td>2.1.2&#160;Smarowanie</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td class="col-rodz">&#160;</td>
        <td>2.1.3&#160;Tłumienie pulsacji</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="PressurePulsationPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td class="col-rodz">&#160;</td>
        <td>2.1.4&#160;Wydajność</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="PumpOperationPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td class="col-rodz">&#160;</td>
        <td>2.1.5&#160;Zawór bezpieczeństwa</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>

      <!-- 2.2 Zbiornik -->
      <tr>
        <td class="urz-cell" rowspan="9"><strong>2.2<br/>Zbiornik</strong><br/>Pojemność<br/>zbiornika<br/>............</td>
        <td class="col-rodz" rowspan="9">&#160;</td>
        <td>2.2.1&#160;Pokrywa otworu wlewowego</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="TankConditionPassed"/></xsl:call-template>
        <td class="col-uw" rowspan="9"><xsl:value-of select="TankSectionNotes"/></td>
      </tr>
      <tr>
        <td>2.2.2&#160;System uniemożliwiający nadlub podciśnienie</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>2.2.3&#160;Mieszanie cieczy</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="AgitatorOperationPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>2.2.4&#160;Wstępne filtrowanie&#160;&#160;w tym sito wlewowe</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="SuctionFilterPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>2.2.5&#160;Wskaźnik poziomu cieczy</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="LevelIndicatorPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>2.2.6&#160;Zawór spustowy zbiornika</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="FlushingSystemPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>
          &#x25A1; Przepłukiwanie<br/>
          2.2.7&#160;Przepłukiwanie stan techniczny i działanie
        </td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>
          &#x25A1; Rozcieńcz.<br/>
          2.2.8&#160;Rozcieńczacz&#160;stan techniczny i działanie
        </td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>
          &#x25A1; Urządz. myjące<br/>
          2.2.9&#160;Urządzenie myjące stan techn. i działanie
        </td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>

    </tbody>
  </table>
  <div style="font-size:5.5pt; margin-top:2mm; color:#555">* Niepotrzebne skreślić</div>
</div><!-- /page1 -->


<!-- ============================================================ PAGE 2 ============================================================ -->
<div class="page" id="str2">

  <div class="cont-hdr">
    <span>Protokół nr: <strong><xsl:value-of select="ProtocolNumber"/></strong></span>
    <span><xsl:value-of select="ClientName"/> / <xsl:value-of select="CropSprayerName"/> (nr ser. <xsl:value-of select="CropSprayerSerialNumber"/>)</span>
    <span>Data: <xsl:call-template name="fmtDate"><xsl:with-param name="d" select="InspectionDate"/></xsl:call-template></span>
  </div>

  <table class="it">
    <thead>
      <tr>
        <th class="col-urz" rowspan="2">Urządzenie opryskiwacza</th>
        <th class="col-rodz" rowspan="2">Rodzaj wyposażenia</th>
        <th class="col-pred" rowspan="2">Przedmiot badań</th>
        <th colspan="2">Ocena przy wyłączonym napędzie</th>
        <th colspan="2">Ocena przy włączonym napędzie</th>
        <th class="col-uw" rowspan="2">Uwagi i zalecenia</th>
      </tr>
      <tr>
        <th class="chk">wada</th><th class="chk">w normie</th>
        <th class="chk">wada</th><th class="chk">w normie</th>
      </tr>
    </thead>
    <tbody>

      <!-- 2.3 Urządzenia pomiarowo-sterujące -->
      <tr>
        <td class="urz-cell" rowspan="6"><strong>2.3<br/>Urządzenia<br/>pomiarowo&#8209;<br/>sterujące</strong></td>
        <td class="col-rodz" rowspan="3">
          &#x25A1; Manometr<br/>&#x25A1; Komputer
        </td>
        <td>2.3.1&#160;Średnica obudowy</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="ManometerDialSizePassed"/></xsl:call-template>
        <td class="col-uw" rowspan="6"><xsl:value-of select="MeasuringSectionNotes"/></td>
      </tr>
      <tr>
        <td>2.3.2&#160;Zakres wskazań, działka elementarna</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="ManometerPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>2.3.3&#160;Stabilność wskazań manometru</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td class="col-rodz" rowspan="3">&#160;</td>
        <td>2.3.4&#160;Błąd pomiaru</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>2.3.5&#160;Stabilność i powtarzalność ciśnienia</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>2.3.6&#160;Zawory (funkcjonowanie)</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="ConnectionsSealingPassed"/></xsl:call-template>
      </tr>

      <!-- 2.4 Układ cieczowy -->
      <tr>
        <td class="urz-cell" rowspan="2"><strong>2.4 Układ<br/>cieczowy</strong></td>
        <td class="col-rodz" rowspan="2">&#160;</td>
        <td>2.4.1&#160;Szczelność i stan techniczny</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="PipesConditionPassed"/></xsl:call-template>
        <td class="col-uw" rowspan="2"><xsl:value-of select="PipingSectionNotes"/></td>
      </tr>
      <tr>
        <td>2.4.2&#160;Zabezpieczenie przed samoopryskiem</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>

      <!-- 2.5 System filtracji -->
      <tr>
        <td class="urz-cell" rowspan="2"><strong>2.5 System<br/>filtracji</strong></td>
        <td class="col-rodz" rowspan="2">&#160;</td>
        <td>2.5.1&#160;Kompletność, stan techniczny i wielkość oczek po stronie tłocznej</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="PressureFilterPassed"/></xsl:call-template>
        <td class="col-uw" rowspan="2"><xsl:value-of select="FiltrationSectionNotes"/></td>
      </tr>
      <tr>
        <td>2.5.2&#160;Filtry rozpylaczy — stan i kompletność</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="NozzleFiltersPassed"/></xsl:call-template>
      </tr>

      <!-- 2.6 Belka polowa -->
      <tr>
        <td class="urz-cell" rowspan="9"><strong>2.6<br/>Belka polowa<br/>opryskiwacza</strong><br/>Szerokość<br/>m.......... <br/><br/>Mokra &#x25A1;<br/>Sucha &#x25A1;<br/><br/>Mechanizm<br/>tłumienia wahań<br/>belki &#x25A1;</td>
        <td class="col-rodz" rowspan="9">&#160;</td>
        <td>2.6.1&#160;Stabilność i stan techniczny</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="FieldBoomConditionPassed"/></xsl:call-template>
        <td class="col-uw" rowspan="9"><xsl:value-of select="BoomSectionNotes"/></td>
      </tr>
      <tr>
        <td>2.6.2&#160;Składanie i stan techniczny</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>2.6.3&#160;Blokada belki polowej, działanie</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>2.6.4&#160;Regulacja wysokości działanie</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="BoomHeightPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>2.6.5&#160;Położenie względem powierzchni</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>2.6.6&#160;Ustawienie rozpylaczy</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="BoomSymmetryPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>2.6.7&#160;Mechanizm odchylania-powrotu, działanie</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="BoomStabilityPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td>2.6.8&#160;Tłumienie wahań belki, działanie</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>2.6.9&#160;Zawory przeciwkroplowe, działanie</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>

      <!-- 2.7 Sekcje (sadowniczy) -->
      <tr>
        <td class="urz-cell" rowspan="2"><strong>2.7 Sekcje<br/>(sadowniczy)</strong><br/>Liczba sekcji<br/><xsl:value-of select="SectionCount"/></td>
        <td class="col-rodz" rowspan="2">&#160;</td>
        <td>2.7.1&#160;Ustawienie rozpylaczy</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="NozzleUniformityPassed"/></xsl:call-template>
        <td class="col-uw" rowspan="2">&#160;</td>
      </tr>
      <tr>
        <td>2.7.2&#160;Zawory przeciwkroplowe, działanie</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="NozzleFlowRatePassed"/></xsl:call-template>
      </tr>

      <!-- 2.8 Rozpylacze (polowy) -->
      <tr>
        <td class="urz-cell" rowspan="7"><strong>2.8<br/>Rozpylacze<br/>(polowy)</strong><br/><br/>Cechy i<br/>oznaczenie:<br/>............</td>
        <td class="col-rodz" rowspan="7">&#160;</td>
        <td>2.8.1&#160;Stan techniczny typ, rozmiar, kąt, materiał</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="NozzleConditionPassed"/></xsl:call-template>
        <td class="col-uw" rowspan="7"><xsl:value-of select="NozzlesSectionNotes"/>
          <xsl:if test="CoefficientOfVariation != ''"><br/>CV = <xsl:value-of select="CoefficientOfVariation"/>%</xsl:if>
        </td>
      </tr>
      <tr>
        <td>2.8.2&#160;Filtry rozpylaczy, typ, rozmiar</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="NozzleConditionPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td style="font-weight:bold">2.8.3&#160;Sprawdzenie dystrybucji cieczy jedną z metod:</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td style="font-size:6pt;padding-left:4mm">2.8.3.1 Pomiar <strong>na ręcznym stole rowkowym:</strong><br/>% rynienk z odchyleniem &gt;15%: ...........</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td style="font-size:6pt;padding-left:4mm">2.8.3.2 Pomiar <strong>na elektronicznym stole rowkowym:</strong> CV%: ........%</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="TransverseDistributionPassed"/></xsl:call-template>
      </tr>
      <tr>
        <td style="font-size:6pt;padding-left:4mm">2.8.3.3 Jednoczesny pomiar <strong>natężenia wypływu cieczy z rozpylaczy</strong></td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>
      <tr>
        <td>2.8.4&#160;Pomiar spadku wartości ciśnienia**</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
      </tr>

      <!-- 2.9 Rozpylacze (sadowniczy) -->
      <tr>
        <td class="urz-cell" rowspan="2"><strong>2.9 Rozpylacze<br/>(sadowniczy)</strong><br/>Cechy i<br/>oznaczenie:<br/>............</td>
        <td class="col-rodz" rowspan="2">&#160;</td>
        <td>2.9.1&#160;Stan techniczny, typ, rozmiar, kąt, materiał</td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="OrchardSprayerConditionPassed"/></xsl:call-template>
        <td class="col-uw" rowspan="2">&#160;</td>
      </tr>
      <tr>
        <td>2.9.2&#160;Jednoczesny <strong>pomiar natężenia wypływu cieczy z rozpylaczy</strong></td>
        <xsl:call-template name="wn2"><xsl:with-param name="v" select="AirStreamDirectionPassed"/></xsl:call-template>
      </tr>

      <!-- 2.10 Wentylator -->
      <tr>
        <td class="urz-cell"><strong>2.10 Wentylator</strong><br/>Typ........</td>
        <td class="col-rodz">&#160;</td>
        <td>2.10.1&#160;Stan techniczny i urządzenia sterujące</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="chk">&#x25A1;</td><td class="chk">&#x25A1;</td>
        <td class="col-uw">&#160;</td>
      </tr>

    </tbody>
  </table>

  <!-- MANOMETER COMPARISON TABLE -->
  <div style="margin-top:3mm">
    <div class="bttitle">Różnice wskazań pomiędzy badanym manometrem a manometrem wzorcowym [2.3.4]</div>
    <table class="manotable">
      <thead>
        <tr>
          <th colspan="2" style="text-align:center">Wskazania manometru wzorcowego (MPa)</th>
          <th rowspan="2" style="width:35mm">Odchylenie wskazań (%)</th>
        </tr>
        <tr>
          <th>Manometr badany</th>
          <th>Manometr wzorcowy</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>
            <xsl:choose>
              <xsl:when test="ManometerReading2Bar != ''"><xsl:value-of select="format-number(number(ManometerReading2Bar) * 0.1,'0.00')"/> MPa</xsl:when>
              <xsl:otherwise>&#160;</xsl:otherwise>
            </xsl:choose>
          </td>
          <td>0,20 MPa</td>
          <td>&#160;</td>
        </tr>
        <tr>
          <td>
            <xsl:choose>
              <xsl:when test="ManometerReading4Bar != ''"><xsl:value-of select="format-number(number(ManometerReading4Bar) * 0.1,'0.00')"/> MPa</xsl:when>
              <xsl:otherwise>&#160;</xsl:otherwise>
            </xsl:choose>
          </td>
          <td>0,40 MPa</td>
          <td>&#160;</td>
        </tr>
        <tr>
          <td>
            <xsl:choose>
              <xsl:when test="ManometerReading6Bar != ''"><xsl:value-of select="format-number(number(ManometerReading6Bar) * 0.1,'0.00')"/> MPa</xsl:when>
              <xsl:otherwise>&#160;</xsl:otherwise>
            </xsl:choose>
          </td>
          <td>0,60 MPa</td>
          <td>&#160;</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- NOZZLE FLOW TABLE -->
  <div style="margin-top:2mm">
    <div class="bttitle">Pomiar natężenia wypływu cieczy z rozpylaczy w opryskiwaczu polowym lub sadowniczym [2.8.3.3 lub 2.9.2]</div>
    <table class="nozzletable">
      <tbody>
        <tr>
          <td class="nlbl">Nr rozpylacza</td>
          <td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td>
          <td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td>
        </tr>
        <tr>
          <td class="nlbl">Odchylenie od wart. nominalnej (%)</td>
          <td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td>
          <td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td>
        </tr>
        <tr>
          <td class="nlbl">Nr rozpylacza</td>
          <td>13</td><td>14</td><td>15</td><td>16</td><td>17</td><td>18</td>
          <td>19</td><td>20</td><td>21</td><td>22</td><td>23</td><td>24</td>
        </tr>
        <tr>
          <td class="nlbl">Odchylenie od wart. nominalnej (%)</td>
          <td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td>
          <td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td>
        </tr>
        <tr>
          <td class="nlbl">Nr rozpylacza</td>
          <td>25</td><td>26</td><td>27</td><td>28</td><td>29</td><td>30</td>
          <td>31</td><td>32</td><td>33</td><td>34</td><td>35</td><td>36</td>
        </tr>
        <tr>
          <td class="nlbl">Odchylenie od wart. nominalnej (%)</td>
          <td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td>
          <td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td><td>&#160;</td>
        </tr>
      </tbody>
    </table>
  </div>

  <div class="footnote">
    * Niepotrzebne skreślić<br/>
    ** Pomiaru (pkt 2.8.4) nie przeprowadza się, jeżeli został przeprowadzony pomiar nierównomierności rozkładu poprzecznego na stole rowkowym<br/>
    Protokół opracowano w ramach zadania nr 2.4 „Opracowanie i ocena metod ograniczania ryzyka związanego ze stosowaniem środków ochrony roślin",
    Programu Wieloletniego „Działania na rzecz poprawy konkurencyjności i innowacyjności sektora agrotechnicznego z uwzględnieniem jakości
    i bezpieczeństwa żywności oraz ochrony środowiska naturalnego", finansowanego przez MRiRW
  </div>

</div><!-- /page2 -->

</body>
</html>
  </xsl:template>
</xsl:stylesheet>
