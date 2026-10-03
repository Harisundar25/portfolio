/**
 * SVG path for a flat Archimedean spiral — the shape of a Qi transmitter coil.
 * Coordinates are centred on (0, 0).
 */
export function buildCoilPath({ turns = 9, rMin = 20, rMax = 165, steps = 720 } = {}) {
  const thetaMax = turns * 2 * Math.PI;
  const growth = (rMax - rMin) / thetaMax;
  const points = [];
  for (let i = 0; i <= steps; i += 1) {
    const theta = (i / steps) * thetaMax;
    const r = rMin + growth * theta;
    points.push(`${(r * Math.cos(theta)).toFixed(2)} ${(r * Math.sin(theta)).toFixed(2)}`);
  }
  return `M${points.join("L")}`;
}
